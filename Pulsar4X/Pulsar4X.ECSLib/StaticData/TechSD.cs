﻿using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public enum ResearchCategories
    {
        BiologyGenetics,
        ConstructionProduction,
        DefensiveSystems,
        EnergyWeapons,
        LogisticsGroundCombat,
        MissilesKineticWeapons,
        PowerAndPropulsion,
        SensorsAndFireControl,
        FromStaticData00,
        FromStaticData01,
        FromStaticData02,
        FromStaticData03,
        FromStaticData04,
        FromStaticData05,
        FromStaticData06,
        FromStaticData07,
        FromStaticData08,
        FromStaticData09,
    }

    [StaticDataAttribute(true, IDPropertyName = "ID")]
    public struct TechSD
    {
        public string Name;
        public string Description;
        public ResearchCategories Category;
        public Guid ID;
        public List<Guid> Requirements;
        public int Cost;
    }
}
