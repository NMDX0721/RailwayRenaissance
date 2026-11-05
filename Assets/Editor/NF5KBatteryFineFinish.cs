using UnityEditor;
using UnityEngine;

public static class NF5KBatteryFineFinish
{
    public static void Apply(Transform root,Transform shell,Transform tray,Transform cover,Material paint,Material steel,Material rubber,Material cell,Material red)
    {
        void Box(string name,Vector3 p,Vector3 size,Material m,Transform parent,float radius=.001f){var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer),typeof(BoxCollider));o.transform.SetParent(parent,false);o.transform.localPosition=p;o.layer=30;o.GetComponent<MeshFilter>().sharedMesh=NF5KPassengerInterior.RoundedBox(size,radius);o.GetComponent<Renderer>().sharedMaterial=m;o.GetComponent<BoxCollider>().size=size;}
        void Rod(string name,Vector3 a,Vector3 b,float diameter,Material m,Transform parent){var o=GameObject.CreatePrimitive(PrimitiveType.Cylinder);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=(a+b)/2;o.transform.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);o.transform.localScale=new Vector3(diameter,(b-a).magnitude/2,diameter);o.GetComponent<Renderer>().sharedMaterial=m;o.layer=30;Object.DestroyImmediate(o.GetComponent<Collider>());o.AddComponent<BoxCollider>();}
        void Screw(Vector3 p,Transform parent){Rod("Washer",p+Vector3.right*.001f,p-Vector3.right*.001f,.013f,steel,parent);Rod("Captive_Head",p-Vector3.right*.001f,p-Vector3.right*.004f,.009f,steel,parent);Box("Driver_Recess",p-Vector3.right*.0042f,new Vector3(.0005f,.0014f,.006f),rubber,parent);}
        foreach(int side in new[]{-1,1}){
            Box("Folded_Casing_Lip",new Vector3(-.169f,0,side*.286f),new Vector3(.020f,.318f,.0025f),paint,shell);
            Box("Cover_Seal",new Vector3(-.180f,0,side*.281f),new Vector3(.004f,.30f,.005f),rubber,shell);
            Box("Cover_Return_Fold",new Vector3(-.177f,0,side*.289f),new Vector3(.018f,.30f,.002f),paint,cover);
            Box("Pressed_Cover_Bead",new Vector3(-.187f,-.055f,side*.259f),new Vector3(.003f,.155f,.005f),paint,cover);
            // Separate outer race/flanges give the guide a folded channel section rather than a bar.
            foreach(float y in new[]{-.130f,-.100f})Box("Rail_Return_Flange",new Vector3(0,y,side*.258f),new Vector3(.31f,.003f,.026f),steel,shell);
            for(int i=0;i<5;i++)Rod("Guide_Roller",new Vector3(-.112f+i*.055f,-.111f,side*.251f),new Vector3(-.112f+i*.055f,-.111f,side*.244f),.013f,steel,shell);
            foreach(float x in new[]{-.11f,.11f})Box("Tray_Rivet",new Vector3(x,-.083f,side*.236f),new Vector3(.008f,.008f,.003f),steel,tray);
            // Compact draw-latch structure: keeper, pivoted base, lever and wire bail.
            float z=side*.243f;
            Box("Latch_Keeper",new Vector3(-.192f,.152f,z),new Vector3(.010f,.014f,.021f),steel,shell);
            Box("Latch_Base",new Vector3(-.190f,.113f,z),new Vector3(.006f,.033f,.020f),steel,cover);
            Rod("Latch_Pivot",new Vector3(-.198f,.100f,z-.013f),new Vector3(-.198f,.100f,z+.013f),.006f,steel,cover);
            Box("Latch_Lever",new Vector3(-.201f,.116f,z),new Vector3(.006f,.032f,.014f),steel,cover,.002f);
            foreach(float dz in new[]{-.008f,.008f})Rod("Latch_Bail",new Vector3(-.202f,.123f,z+dz),new Vector3(-.202f,.151f,z+dz),.003f,steel,cover);
            Rod("Latch_Bail_End",new Vector3(-.202f,.151f,z-.008f),new Vector3(-.202f,.151f,z+.008f),.003f,steel,cover);
            Screw(new Vector3(-.189f,-.124f,side*.238f),cover);
        }
        foreach(float y in new[]{-.152f,.150f})Box("Gasket_Cross_Seal",new Vector3(-.179f,y,0),new Vector3(.004f,.004f,.55f),rubber,shell);
        foreach(float y in new[]{-.132f,.021f})Box("Pressed_Cover_Bead",new Vector3(-.187f,y,0),new Vector3(.003f,.005f,.52f),paint,cover);
        // Raised mounting straps and isolation pads, with visible washer/head/slot detail.
        foreach(float z in new[]{-.25f,.25f}){
            Box("Mount_Isolation_Pad",new Vector3(.06f,.211f,z),new Vector3(.12f,.006f,.048f),rubber,shell);
            foreach(float x in new[]{-.08f,.12f}){Rod("Mount_Washer",new Vector3(x,.211f,z),new Vector3(x,.215f,z),.019f,steel,shell);Rod("Mount_Head",new Vector3(x,.206f,z),new Vector3(x,.211f,z),.012f,steel,shell);}
        }
        foreach(float z in new[]{-.117f,.117f}){
            // The dark cap is distinct from the moulded case, with small feet and stiffening ribs.
            Box("Case_Lid_Seam",new Vector3(-.121f,.050f,z),new Vector3(.002f,.0025f,.204f),rubber,tray);
            for(int i=0;i<4;i++)Box("Moulded_Case_Rib",new Vector3(-.121f,-.035f,z-.073f+i*.049f),new Vector3(.004f,.125f,.004f),cell,tray,.0015f);
            foreach(float x in new[]{-.095f,.095f})Box("Moulded_Case_Foot",new Vector3(x,-.113f,z),new Vector3(.025f,.008f,.18f),cell,tray);
            foreach(int sign in new[]{-1,1}){
                Rod("Terminal_Boot_Collar",new Vector3(sign*.073f,.079f,z),new Vector3(sign*.073f,.085f,z),.033f,sign>0?red:rubber,tray);
                Box("Terminal_Embossed_Minus",new Vector3(sign*.073f,.101f,z),new Vector3(.014f,.001f,.002f),steel,tray);
                if(sign>0)Box("Terminal_Embossed_Plus",new Vector3(sign*.073f,.1012f,z),new Vector3(.002f,.001f,.014f),steel,tray);
            }
            foreach(float x in new[]{-.130f,.130f}){
                Rod("Hold_Down_Tie_Rod",new Vector3(x,-.11f,z+.055f),new Vector3(x,.110f,z+.055f),.005f,steel,tray);
                Rod("Hold_Down_Nut",new Vector3(x,.101f,z+.055f),new Vector3(x,.106f,z+.055f),.011f,steel,tray);
            }
            Box("Battery_Service_Label",new Vector3(-.123f,-.025f,z),new Vector3(.001f,.041f,.071f),steel,tray);
        }
        Box("Cover_ID_Plate",new Vector3(-.187f,-.104f,0),new Vector3(.0015f,.026f,.10f),steel,cover);
        var font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        font.RequestCharactersInTexture("BATTERYB012",64);
        string labelPath="Assets/Resources/Cab/Battery_Label.mat";var label=AssetDatabase.LoadAssetAtPath<Material>(labelPath);
        if(label==null){label=new Material(Shader.Find("RailRevival/BatteryLabel"));AssetDatabase.CreateAsset(label,labelPath);}
        label.mainTexture=font.material.mainTexture;EditorUtility.SetDirty(label);
        void Text(string value,Vector3 p,Transform parent,float scale){var o=new GameObject("Stamped_ID");o.transform.SetParent(parent,false);o.transform.localPosition=p;o.transform.localRotation=Quaternion.Euler(0,90,0);o.layer=30;var t=o.AddComponent<TextMesh>();t.font=font;t.fontSize=64;t.characterSize=scale;t.text=value;t.anchor=TextAnchor.MiddleCenter;t.color=new Color(.06f,.07f,.06f);o.GetComponent<Renderer>().sharedMaterial=label;}
        Text("BATTERY",new Vector3(-.188f,-.104f,0),cover,.0032f);
        int index=1;foreach(float z in new[]{-.117f,.117f})Text("B0"+index++,new Vector3(-.124f,-.025f,z),tray,.005f);
        Grain(paint,"Paint",false);Grain(steel,"Steel",true);paint.SetFloat("_Smoothness",.23f);cell.SetFloat("_Smoothness",.24f);rubber.SetFloat("_Smoothness",.12f);
    }
    static void Grain(Material material,string name,bool brushed)
    {
        const int n=128;string path="Assets/Resources/Cab/Battery_"+name+"_Grain.asset";var t=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if(t==null){t=new Texture2D(n,n,TextureFormat.RGBA32,true);AssetDatabase.CreateAsset(t,path);}
        var random=new System.Random(6021);var pixels=new Color[n*n];
        for(int y=0;y<n;y++)for(int x=0;x<n;x++){float v=.97f+(float)random.NextDouble()*.03f;if(brushed)v-=.018f*Mathf.Sin(y*1.7f);pixels[y*n+x]=new Color(v,v,v,1);}
        t.SetPixels(pixels);t.Apply();t.wrapMode=TextureWrapMode.Repeat;t.filterMode=FilterMode.Bilinear;material.SetTexture("_BaseMap",t);material.SetTextureScale("_BaseMap",new Vector2(5,5));EditorUtility.SetDirty(t);EditorUtility.SetDirty(material);
    }
}
