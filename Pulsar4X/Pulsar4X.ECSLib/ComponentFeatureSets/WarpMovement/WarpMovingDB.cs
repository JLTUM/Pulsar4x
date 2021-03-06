﻿using System;
using System.Data.SqlClient;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Pulsar4X.Vectors;

namespace Pulsar4X.ECSLib
{


    /// <summary>
    /// This datablob gets added to an entity when that entity is doing non-newtonion translation type movement.
    /// It gets removed from the entity once the entity has finished the translation. 
    /// </summary>
    public class WarpMovingDB : BaseDataBlob
    {
        [JsonProperty]
        public DateTime LastProcessDateTime = new DateTime();

        public Vector3 SavedNewtonionVector { get; internal set; }
        [JsonProperty]
        public Vector3 SavedNewtonionVector_AU
        {
            get => Distance.MToAU(SavedNewtonionVector);
            internal set => SavedNewtonionVector = Distance.AuToMt(value);
        }

        [JsonProperty]
        public Vector3 EntryPointAbsolute { get; internal set; }
        public Vector3 TranslateEntryAbsolutePoint_AU
        {
            get => Distance.MToAU(EntryPointAbsolute);
            internal set => EntryPointAbsolute = Distance.AuToMt(value);
        }
        [JsonProperty]
        public Vector3 ExitPointAbsolute { get; internal set; }
        public Vector3 TranslateExitPoint_AU => Distance.MToAU(ExitPointAbsolute);

        [JsonProperty]
        public Vector3 ExitPointRalitive { get; internal set; }

        [JsonProperty]
        public float Heading_Radians { get; internal set; }
        [JsonProperty]
        public DateTime EntryDateTime { get; internal set; }
        [JsonProperty]
        public DateTime PredictedExitTime { get; internal set; }

        [JsonProperty]
        internal Vector3 CurrentNonNewtonionVectorMS { get; set; }

        /// <summary>
        /// m/s
        /// </summary>
        [JsonProperty]
        internal Vector3 ExpendDeltaV { get; set; }
        internal Vector3 ExpendDeltaV_AU {
            get => Distance.MToAU(ExpendDeltaV);
            set => ExpendDeltaV = Distance.AuToMt(value);
        }

        [JsonProperty]
        internal bool IsAtTarget { get; set; }

        [JsonProperty]
        internal Entity TargetEntity;
        [JsonIgnore] //don't store datablobs, we catch this on deserialization. 
        internal PositionDB TargetPositionDB;

        public WarpMovingDB()
        {
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="T:Pulsar4X.ECSLib.TranslateMoveDB"/> class.
        /// Use this one to move to a specific postion vector. 
        /// </summary>
        /// <param name="targetPosition_m">Target position in Meters.</param>
        public WarpMovingDB(Entity thisEntity, Vector3 targetPosition_m)
        {
            ExitPointAbsolute = targetPosition_m;
            
            Vector3 startPosAbsolute_m = Entity.GetPosition_m(thisEntity, thisEntity.StarSysDateTime, false);
            Vector3 currentVec_m = Entity.GetVelocity_m(thisEntity, thisEntity.StarSysDateTime);
                    
            ExitPointAbsolute = targetPosition_m;
            EntryPointAbsolute = startPosAbsolute_m;
            EntryDateTime = thisEntity.Manager.ManagerSubpulses.StarSysDateTime;
            ExitPointRalitive = Vector3.Zero;
            //PredictedExitTime = targetIntercept.atDateTime;
            SavedNewtonionVector = currentVec_m;
            TargetEntity = null;
            
            Heading_Radians = (float)Vector3.AngleBetween(startPosAbsolute_m, ExitPointAbsolute);
            
            Heading_Radians = (float)Math.Atan2(targetPosition_m.Y, targetPosition_m.X);
        }

        /// <summary>
        /// Use this to move to an entity that has an orbitDB
        /// </summary>
        /// <param name="targetPositiondb"></param>
        /// <param name="offsetPosition">normaly you want to move to a position next to the entity, this is
        /// a position ralitive to the entity you're wanting to move to</param>
        public WarpMovingDB(Entity thisEntity, Entity targetEntity, Vector3 offsetPosition)
        {
            if(!targetEntity.HasDataBlob<OrbitDB>())
                throw new NotImplementedException("Currently we can only predict the movement of stable orbits - target must have an orbitDB");
            (Vector3 position, DateTime atDateTime) targetIntercept = OrbitProcessor.GetInterceptPosition_m
            (
                thisEntity, 
                targetEntity.GetDataBlob<OrbitDB>(), 
                thisEntity.StarSysDateTime
            );
            
            Vector3 startPosAbsolute_m = Entity.GetPosition_m(thisEntity, thisEntity.StarSysDateTime, false);
            Vector3 currentVec_m = Entity.GetVelocity_m(thisEntity, thisEntity.StarSysDateTime);
                    
            ExitPointAbsolute = targetIntercept.position + offsetPosition;
            EntryPointAbsolute = startPosAbsolute_m;
            EntryDateTime = thisEntity.Manager.ManagerSubpulses.StarSysDateTime;
            ExitPointRalitive = offsetPosition;
            PredictedExitTime = targetIntercept.atDateTime;
            SavedNewtonionVector = currentVec_m;
            TargetEntity = targetEntity;
            
            Heading_Radians = (float)Vector3.AngleBetween(startPosAbsolute_m, ExitPointAbsolute);
        }
        
        public WarpMovingDB(WarpMovingDB db)
        {
            LastProcessDateTime = db.LastProcessDateTime;
            SavedNewtonionVector = db.SavedNewtonionVector;
            EntryPointAbsolute = db.EntryPointAbsolute;
            ExitPointAbsolute = db.ExitPointAbsolute;
            CurrentNonNewtonionVectorMS = db.CurrentNonNewtonionVectorMS;
            ExpendDeltaV = db.ExpendDeltaV;
            IsAtTarget = db.IsAtTarget;
            TargetEntity = db.TargetEntity;

            TargetPositionDB = db.TargetPositionDB;

        }
        // JSON deserialization callback.
        [OnDeserialized]
        private void Deserialized(StreamingContext context)
        {

            if (TargetEntity != null)
            {

                var game = (Game)context.Context;
                game.PostLoad += (sender, args) =>
                {
                    TargetPositionDB = TargetEntity.GetDataBlob<PositionDB>();
                };
            }
        }

        public override object Clone()
        {
            return new WarpMovingDB(this);
        }
    }
}
