using UnityEngine;

namespace RailwayRenaissance.Cab
{
    // Presentation adapter only: no control or protection writes.
    public sealed class CabVigilanceIndicator : MonoBehaviour
    {
        [SerializeField] CabVigilance source;
        [SerializeField] CabAnnunciatorPanel supply;
        [SerializeField] Renderer lens;
        [SerializeField] Renderer legend;
        MaterialPropertyBlock block;
        public bool Lit {get;private set;}
        public void Configure(CabVigilance input,CabAnnunciatorPanel power,Renderer display,Renderer text){source=input;supply=power;lens=display;legend=text;Refresh();}
        public void Refresh()
        {
            Lit=isActiveAndEnabled&&source!=null&&source.isActiveAndEnabled&&supply!=null&&supply.SupplyAvailable&&(supply.IsTesting||source.Phase>0);
            if(lens==null)return;
            block??=new MaterialPropertyBlock();block.Clear();var amber=new Color(1,.52f,.045f);
            block.SetColor("_BaseColor",amber*(Lit?.12f:.045f)+new Color(.018f,.021f,.022f,0));
            block.SetColor("_EmissionColor",Lit?amber*1.05f:Color.black);lens.SetPropertyBlock(block);
            if(legend!=null){block.Clear();block.SetColor("_BaseColor",Lit?new Color(.10f,.10f,.08f):new Color(.65f,.65f,.59f));block.SetColor("_EmissionColor",Color.black);legend.SetPropertyBlock(block);}
        }
        void LateUpdate()=>Refresh();
        void OnDisable()=>Refresh();
    }
}
