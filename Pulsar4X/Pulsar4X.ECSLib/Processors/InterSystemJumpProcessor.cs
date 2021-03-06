﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    public static class InterSystemJumpProcessor
    {
        //TODO look at turning the entity into a ProtoEntity instead of shifting it to the GlobalManager
        internal static void JumpOut(Game game, SystemEntityJumpPair jumpPair)
        {
            jumpPair.JumpingEntity.Transfer(game.GlobalManager);
            

        }
        internal static void JumpIn(Game game, SystemEntityJumpPair jumpPair)
        {
            jumpPair.JumpingEntity.Transfer(jumpPair.JumpSystem);
        }

        public static void SetJump(Game game, DateTime exitTime, StarSystem entrySystem, DateTime entryTime, Entity jumpingEntity)
        {
            SystemEntityJumpPair jumpPair = new SystemEntityJumpPair
            {
                JumpSystem = entrySystem,
                JumpingEntity = jumpingEntity
            };
            game.GamePulse.AddSystemInteractionInterupt(exitTime, PulseActionEnum.JumpOutProcessor, jumpPair);

            game.GamePulse.AddSystemInteractionInterupt(entryTime, PulseActionEnum.JumpInProcessor, jumpPair);
        }

    }

    internal struct SystemEntityJumpPair
    {
        internal StarSystem JumpSystem;
        internal Entity JumpingEntity;
    } 
}
