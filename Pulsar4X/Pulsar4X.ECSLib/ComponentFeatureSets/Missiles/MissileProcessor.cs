using System.Collections.Generic;

namespace Pulsar4X.ECSLib.ComponentFeatureSets.Missiles
{
    public class MissileProcessor
    {
        public static void LaunchMissile(Entity launchingEntity, Entity targetEntity, MissileLauncherAtb launcherAtb, OrdnanceDesign missileDesign)
        {

            var atDatetime = launchingEntity.Manager.StarSysDateTime;
            var positionDB = launchingEntity.GetDataBlob<PositionDB>();
            Vector3 position = positionDB.AbsolutePosition_m;

            var tgtEntityOrbit = targetEntity.GetDataBlob<OrbitDB>();
            
            //MissileLauncherAtb launcherAtb;
            CargoStorageDB cargo = launchingEntity.GetDataBlob<CargoStorageDB>();
            int numMis = (int)StorageSpaceProcessor.GetAmount(cargo, missileDesign);
            if (numMis < 1)
                return;
            double launchSpeed = launcherAtb.LaunchForce / missileDesign.Mass;
            
            //missileDesign.

            double burnTime = (missileDesign.WetMass - missileDesign.DryMass) / missileDesign.BurnRate;
            double dv = OrbitMath.TsiolkovskyRocketEquation(missileDesign.WetMass, missileDesign.DryMass, missileDesign.ExaustVelocity);
            double avgSpd = launchSpeed + dv * 0.5;
            
            var tgtEstPos = OrbitMath.GetInterceptPosition_m(position, avgSpd, tgtEntityOrbit, atDatetime);
            
            Vector3 parentVelocity = Entity.GetVelocity_m(launchingEntity, launchingEntity.Manager.StarSysDateTime);

            Vector3 tgtEstVector = tgtEstPos.position - position; //future target position
            Vector3 launchVelocity = parentVelocity + tgtEstVector;
            
            List<BaseDataBlob> dataBlobs = new List<BaseDataBlob>();
            dataBlobs.Add((PositionDB)positionDB.Clone());

            var newMissile = Entity.Create(launchingEntity.Manager, launchingEntity.FactionOwner, dataBlobs);
            foreach (var tuple in missileDesign.Components)
            {
                EntityManipulation.AddComponentToEntity(newMissile, tuple.design, tuple.count);
            }
            
            StorageSpaceProcessor.RemoveCargo(cargo, missileDesign, 1);
            var thrusting = new NewtonMoveDB(positionDB.Parent, launchVelocity);
            thrusting.ActionOnDateTime = atDatetime;
            thrusting.DeltaVForManuver_m = Vector3.Normalise(tgtEstPos.position) * dv;
            newMissile.SetDataBlob(thrusting);
            
        }
    }
}