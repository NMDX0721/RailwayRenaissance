using System;
using UnityEngine;

namespace RailwayRenaissance.Cab
{
    [Serializable]
    public sealed class CabSafetyProduct
    {
        public string manufacturer, manufacturerEnglish, series, model, type, agent, charge, pressure, temperature, rating, application, operation, inspection, records;
        public string seriesEnglish,typeEnglish,agentEnglish,applicationEnglish,operationEnglish,inspectionEnglish,recordsEnglish;
        public string Information =>
            manufacturer+"\n"+manufacturerEnglish+"\n"+series+"\n"+seriesEnglish+"\n\n"+
            "产品名称 / PRODUCT\n"+type+"\n"+typeEnglish+"\n型号 / MODEL  "+model+"\n药剂 / AGENT\n"+agent+"\n"+agentEnglish+"\n充装质量 / AGENT CHARGE  "+charge+
            "\n额定压力 / NOMINAL PRESSURE  "+pressure+"\n工作温度 / TEMPERATURE RANGE  "+temperature+"\n灭火级别 / FIRE RATING  "+rating+
            "\n\n适用范围 / APPLICATION\n"+application+"\n"+applicationEnglish+"\n\n操作要点 / OPERATION\n"+operation+"\n"+operationEnglish+
            "\n\n检查与维护 / INSPECTION & MAINTENANCE\n"+inspection+"\n"+inspectionEnglish+"\n\n设备履历 / SERVICE RECORDS\n"+records+"\n"+recordsEnglish;
    }
    public static class CabSafetyCatalog
    {
        static CabSafetyProduct product;
        public static CabSafetyProduct Extinguisher
        {
            get
            {
                if(product==null)
                {
                    var asset=Resources.Load<TextAsset>("Cab/safety_equipment_profile");
                    if(asset==null)throw new InvalidOperationException("Missing safety equipment profile");
                    product=JsonUtility.FromJson<CabSafetyProduct>(asset.text);
                }
                return product;
            }
        }
    }
}
