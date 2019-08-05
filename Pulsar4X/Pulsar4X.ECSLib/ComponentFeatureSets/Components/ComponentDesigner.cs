﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;


namespace Pulsar4X.ECSLib
{
    public enum GuiHint
    {
        None,
        GuiTechSelectionList,
        GuiSelectionMaxMin,
        GuiTextDisplay
        
    }
    
    public class ComponentDesign
    {
        public Guid Guid;
        public int ResearchCostValue;
        public string TypeName; //ie the name in staticData. ie "Newtonion Thruster".
        public string Name; //player defined name. ie "5t 2kn Thruster".
        public string Description;
        public int Mass;
        public int Volume;
        public int HTK;
        public int CrewReq;
        public int BuildPointCost;
        public int CreditCost;
        public Guid CargoTypeID;
        //public int ResearchCostValue;
        public Dictionary<Guid, int> MineralCosts;
        public Dictionary<Guid, int> MaterialCosts;
        public Dictionary<Guid, int> ComponentCosts;
        public ConstructionType ConstructionType;
        public ComponentMountType ComponentMountType;
        //public List<ComponentDesignAtbData> ComponentDesignAttributes;
        public Dictionary<Type, IComponentDesignAttribute> AttributesByType = new Dictionary<Type, IComponentDesignAttribute>();

        public bool HasAttribute<T>()
            where T : IComponentDesignAttribute
        {
            return AttributesByType.ContainsKey(typeof(T));
        }
        
        /// <summary>
        /// Will throw an exception if it doesn't have the type of attribute.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetAttribute<T>() 
            where T : IComponentDesignAttribute
        {
            return (T)AttributesByType[typeof(T)];
        }
        
        public bool TryGetAttribute<T>(out T attribute)
            where T : IComponentDesignAttribute
        {
            if (HasAttribute<T>())
            {
                attribute = (T)AttributesByType[typeof(T)];
                return true;
            }
            attribute = default(T);
            return false;
        }
    }
    
    /// <summary>
    /// This object is what the UI manipulates to create a player design
    /// the ComponentDesignData is then set.
    /// </summary>
    public class ComponentDesigner
    {
        ComponentDesign _design = new ComponentDesign();
        
        public ComponentDesigner(ComponentTemplateSD componentSD, FactionTechDB factionTech)
        {
            var staticData = StaticRefLib.StaticData;
            TypeName = componentSD.Name;
            Name = componentSD.Name;
            Description = componentSD.Description;
            _design.Guid = Guid.NewGuid();
            MassFormula = new ChainedExpression(componentSD.MassFormula, this, factionTech, staticData);
            VolumeFormula = new ChainedExpression(componentSD.VolumeFormula, this, factionTech, staticData);
            CrewFormula = new ChainedExpression(componentSD.CrewReqFormula, this, factionTech, staticData);
            HTKFormula = new ChainedExpression(componentSD.HTKFormula, this, factionTech, staticData);
            ResearchCostFormula = new ChainedExpression(componentSD.ResearchCostFormula, this, factionTech, staticData);
            BuildCostFormula = new ChainedExpression(componentSD.BuildPointCostFormula, this, factionTech, staticData);
            CreditCostFormula = new ChainedExpression(componentSD.CreditCostFormula, this, factionTech, staticData);
            ComponentMountType = componentSD.MountType;
            ConstructionType = componentSD.ConstructionType;
            CargoTypeID = componentSD.CargoTypeID;

            
            Dictionary<Guid, ChainedExpression> mineralCostFormulas = new Dictionary<Guid, ChainedExpression>();
            Dictionary<Guid, ChainedExpression> materalCostFormulas = new Dictionary<Guid, ChainedExpression>();
            Dictionary<Guid, ChainedExpression> componentCostForulas = new Dictionary<Guid, ChainedExpression>();
            foreach (var kvp in componentSD.MineralCostFormula)
            {
      
                if (staticData.CargoGoods.IsMaterial(kvp.Key))
                {
                    materalCostFormulas.Add(kvp.Key, new ChainedExpression(kvp.Value, this, factionTech, staticData));
                }
                else if (staticData.ComponentTemplates.ContainsKey(kvp.Key))
                {
                    componentCostForulas.Add(kvp.Key, new ChainedExpression(kvp.Value, this, factionTech, staticData));
                }
                else if (staticData.CargoGoods.IsMineral(kvp.Key))
                {
                    mineralCostFormulas.Add(kvp.Key, new ChainedExpression(kvp.Value, this, factionTech, staticData));
                }
                else //TODO: log don't crash.
                    throw new Exception("GUID object {" + kvp.Key + "} not found in materialCosting for " + this.TypeName + " This object needs to be either a mineral, material or component defined in the Data folder");

            }

            MineralCostFormulas = mineralCostFormulas;
            MaterialCostFormulas = materalCostFormulas;
            ComponentCostFormulas = componentCostForulas;
            
            foreach (var abilitySD in componentSD.ComponentAbilitySDs)
            {
                ComponentDesignAttribute designAttribute = new ComponentDesignAttribute(this);

                designAttribute.Name = abilitySD.Name;
                designAttribute.Description = abilitySD.Description;
                designAttribute.GuiHint = abilitySD.GuiHint;

                if(abilitySD.AbilityFormula !=  null)
                    designAttribute.Formula = new ChainedExpression(abilitySD.AbilityFormula, designAttribute, factionTech, staticData);

                if (abilitySD.GuidDictionary != null )
                {
                    designAttribute.GuidDictionary = new Dictionary<object, ChainedExpression>();
                    if (designAttribute.GuiHint == GuiHint.GuiTechSelectionList)
                    {
                        foreach (var kvp in abilitySD.GuidDictionary)
                        {
                            if (factionTech.ResearchedTechs.ContainsKey(Guid.Parse(kvp.Key.ToString())))
                            {
                                TechSD techSD = staticData.Techs[Guid.Parse(kvp.Key.ToString())];
                                designAttribute.GuidDictionary.Add(kvp.Key, new ChainedExpression(ResearchProcessor.DataFormula(factionTech, techSD).ToString(), designAttribute, factionTech, staticData));                      
                            }
                        }
                    }
                    else
                    {
                        foreach (var kvp in abilitySD.GuidDictionary)
                        {
                            designAttribute.GuidDictionary.Add(kvp.Key, new ChainedExpression(kvp.Value, designAttribute, factionTech, staticData));
                        }
                    }
                }
                if (designAttribute.GuiHint == GuiHint.GuiSelectionMaxMin)
                {
                    designAttribute.MaxValueFormula = new ChainedExpression(abilitySD.MaxFormula, designAttribute, factionTech, staticData);
                    designAttribute.MinValueFormula = new ChainedExpression(abilitySD.MinFormula, designAttribute, factionTech, staticData);
                    designAttribute.StepValueFormula = new ChainedExpression(abilitySD.StepFormula, designAttribute, factionTech, staticData);
                }
                if (abilitySD.AbilityDataBlobType != null)
                {
                    designAttribute.DataBlobType = Type.GetType(abilitySD.AbilityDataBlobType);        
                }
                
                ComponentDesignAttributes.Add(designAttribute);
            }

            SetMass();
            SetVolume();
            SetCreditCost();
            SetCrew();
            SetHTK();
            SetResearchCost();
            SetBuildCost();
            SetMineralCosts();
            SetMaterialCosts();
            SetComponentCosts();
        }


        
        /// <summary>
        /// "Set" and returns the designdata
        /// this also sets up a research item for the design,
        /// and adds it to the factions designs.
        /// </summary>
        /// <returns></returns>
        public ComponentDesign CreateDesign(Entity factionEntity)
        {
 
            FactionInfoDB faction = factionEntity.GetDataBlob<FactionInfoDB>();
            
            //set up the research
            FactionTechDB factionTech = factionEntity.GetDataBlob<FactionTechDB>();
            TechSD tech = new TechSD();
            tech.ID = Guid.NewGuid();
            tech.Name = _design.Name + " Design Research";
            tech.Description = "Research into building " + _design.Name;
            tech.MaxLevel = 1;
            tech.CostFormula = _design.ResearchCostValue.ToString();

            //TODO: does this save? this is a bit of a hack iirc since static data shouldn't technicaly be created mid game...
            //'tech' is TechSD (static data)
            factionTech.ResearchableTechs.Add(tech, 0); //add it to researchable techs 
            
            foreach (var designAttribute in ComponentDesignAttributes)
            {
                if (designAttribute.DataBlobType != null)
                {
                    if (designAttribute.DataBlobArgs == null)
                        designAttribute.SetValue();  //force recalc.
                                 
                    object[] constructorArgs = designAttribute.DataBlobArgs;
                    
                    dynamic attrbute = (IComponentDesignAttribute)Activator.CreateInstance(designAttribute.DataBlobType, constructorArgs);
                    _design.AttributesByType.Add(attrbute.GetType(), attrbute);
                    
                }
            }

            faction.InternalComponentDesigns.Add(_design.Guid, _design);
            return _design;
            
        }
        

        public string TypeName
        {
            get { return _design.TypeName; }
            set { _design.TypeName = value; }
        }

        public string Name 
        {             
            get { return _design.Name; }
            set { _design.Name = value; } }
        public string Description 
        {             
            get { return _design.Description; }
            set { _design.Description = value; } 
        }



        public int MassValue
        {
            get { return _design.Mass; }
        }
        internal ChainedExpression MassFormula { get; set; }
        public void SetMass()
        {
            MassFormula.Evaluate();
            _design.Mass = MassFormula.IntResult;
        }

        public int VolumeValue { get { return _design.Volume; } }//TODO: check units are @SI UNITS kg/m^3
        internal ChainedExpression VolumeFormula { get; set; }
        public void SetVolume()
        {
            VolumeFormula.Evaluate();
            _design.Volume = VolumeFormula.IntResult;
        }


        public int HTKValue { get { return _design.HTK; } }
        internal ChainedExpression HTKFormula { get; set; }
        public void SetHTK()
        {
            HTKFormula.Evaluate();
            _design.HTK = HTKFormula.IntResult;
        }

        public int CrewReqValue { get { return _design.CrewReq ; } }
        internal ChainedExpression CrewFormula { get; set; }
        public void SetCrew()
        {
            CrewFormula.Evaluate();
            _design.CrewReq = CrewFormula.IntResult;
        }

        public int ResearchCostValue { get { return _design.ResearchCostValue; } }
        internal ChainedExpression ResearchCostFormula { get; set; }
        public void SetResearchCost()
        {
            ResearchCostFormula.Evaluate();
            _design.ResearchCostValue = ResearchCostFormula.IntResult;
        }

        public int BuildPointCostValue { get { return _design.BuildPointCost; } }
        internal ChainedExpression BuildCostFormula { get; set; }
        public void SetBuildCost()
        {
            BuildCostFormula.Evaluate();
            _design.BuildPointCost = BuildCostFormula.IntResult;
        }

        
        public Dictionary<Guid, int> MineralCostValues {get{return  _design.MineralCosts;}}
        internal Dictionary<Guid, ChainedExpression> MineralCostFormulas { get; set; }
        public void SetMineralCosts()
        {
            Dictionary<Guid,int> dict = new Dictionary<Guid, int>();
            foreach (var kvp in MineralCostFormulas)
            {
                kvp.Value.Evaluate();
                dict.Add(kvp.Key, kvp.Value.IntResult);  
            }
            _design.MineralCosts = dict;
        }
        
        public Dictionary<Guid, int> MaterialCostValues {get{return  _design.MaterialCosts;}}
        internal Dictionary<Guid, ChainedExpression> MaterialCostFormulas { get; set; }
        public void SetMaterialCosts()
        {
            Dictionary<Guid,int> dict = new Dictionary<Guid, int>();
            foreach (var kvp in MaterialCostFormulas)
            {
                kvp.Value.Evaluate();
                dict.Add(kvp.Key, kvp.Value.IntResult);  
            }
            _design.MineralCosts = dict;
        }
        
        public Dictionary<Guid, int> ComponentCostValues {get{return  _design.ComponentCosts;}}
        internal Dictionary<Guid, ChainedExpression> ComponentCostFormulas { get; set; }
        public void SetComponentCosts()
        {
            Dictionary<Guid,int> dict = new Dictionary<Guid, int>();
            foreach (var kvp in ComponentCostFormulas)
            {
                kvp.Value.Evaluate();
                dict.Add(kvp.Key, kvp.Value.IntResult);  
            }
            _design.ComponentCosts = dict;
        }
        
        
        public int CreditCostValue { get { return _design.CreditCost; } }
        internal ChainedExpression CreditCostFormula { get; set; }
        public void SetCreditCost()
        {
            CreditCostFormula.Evaluate();
            _design.CreditCost = CreditCostFormula.IntResult;
        }

        public ComponentMountType ComponentMountType
        {
            get { return _design.ComponentMountType;} 
            internal set { _design.ComponentMountType = value; } }
        public ConstructionType ConstructionType
        {
            get { return _design.ConstructionType; }
            internal set { _design.ConstructionType = value; }
        }
        public Guid CargoTypeID 
        {             
            get { return _design.CargoTypeID; }
            internal set { _design.CargoTypeID = value; } 
        }
        
        public List<ComponentDesignAttribute> ComponentDesignAttributes = new List<ComponentDesignAttribute>();
        public Dictionary<Type, IComponentDesignAttribute> Attributes
        {
            get { return _design.AttributesByType; }
        }

    }
}