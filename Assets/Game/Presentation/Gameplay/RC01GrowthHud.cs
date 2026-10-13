using System.Collections.Generic;
using UnityEngine;
using Waffle;
using Waffle.RunContent;
using Reverb.Drafts.RC01.Pipeline;

namespace Reverb.Drafts.RC01.Presentation
{
    /// <summary>Read-only presentation of the real run. All actions use the existing command interface.</summary>
    public class RC01GrowthHud : MonoBehaviour
    {
        public GameLoop loop;
        public bool gameplayNextSettlement;
        Font font;
        GUIStyle small, body, title, number, center, button, tiny;
        Texture2D buttonFill, buttonHover;
        string lastResult="";
        readonly Vector2[] choiceScroll=new Vector2[3];
        static readonly Color Ink=new Color(.035f,.065f,.075f,.93f);
        static readonly Color Jade=new Color(.44f,.87f,.78f);
        static readonly Color Muted=new Color(.62f,.72f,.72f);
        static readonly Color Gold=new Color(.95f,.77f,.43f);
        void Awake(){if(loop==null)loop=GetComponent<GameLoop>();}
        void Init()
        {
            if(body!=null)return;
            font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Noto Sans CJK SC","Arial"},24);
            if(font==null)font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            body=Text(16,Color.white);small=Text(13,Muted);tiny=Text(11,Muted);
            title=Text(25,Color.white,FontStyle.Bold);number=Text(34,Color.white);center=Text(23,Color.white);center.alignment=TextAnchor.MiddleCenter;
            buttonFill=Fill(new Color(.13f,.23f,.24f));buttonHover=Fill(new Color(.2f,.38f,.37f));
            button=new GUIStyle(body){alignment=TextAnchor.MiddleCenter,padding=new RectOffset(8,8,4,4)};
            button.normal.background=buttonFill;button.hover.background=buttonHover;button.active.background=buttonHover;
            button.focused.background=buttonHover;button.normal.textColor=Color.white;button.hover.textColor=Color.white;button.active.textColor=Jade;
        }
        GUIStyle Text(int size,Color color,FontStyle style=FontStyle.Normal)
        {return new GUIStyle(GUI.skin.label){font=font,fontSize=size,fontStyle=style,wordWrap=true,normal={textColor=color},padding=new RectOffset(0,0,0,0)};}
        static Texture2D Fill(Color color){var t=new Texture2D(1,1);t.SetPixel(0,0,color);t.Apply();return t;}
        static void RectFill(Rect rect,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;}
        static void Panel(Rect r,Color accent)
        {
            RectFill(new Rect(r.x+3,r.y+4,r.width,r.height),new Color(0,0,0,.15f));
            RectFill(r,Ink);RectFill(new Rect(r.x,r.y,r.width,2),accent);
        }
        void Label(Rect r,string value,GUIStyle style)=>GUI.Label(r,value,style);
        static string Clock(float time){int s=Mathf.FloorToInt(time);return string.Format("{0:00}:{1:00}",s/60,s%60);}
        void Bar(Rect r,float amount,Color color)
        {RectFill(r,new Color(.16f,.22f,.22f));RectFill(new Rect(r.x,r.y,r.width*Mathf.Clamp01(amount),r.height),color);}
        void OnGUI()
        {
            Init();var oldMatrix=GUI.matrix;ImguiScale.Apply();
            var source=GetComponent<IRunContentViewSource>();var commands=GetComponent<IRunContentCommands>();
            var view=source?.ReadView();var sim=loop?.Sim;
            if(sim==null){GUI.matrix=oldMatrix;return;}
            var state=sim.State;float width=ImguiScale.Width,height=ImguiScale.Height;
            Panel(new Rect(24,22,276,108),Jade);
            Label(new Rect(40,32,245,20),"REVERB  /  麦田里的混响",small);
            Label(new Rect(40,58,150,28),"生命",body);
            Label(new Rect(170,57,110,25),state.Hp+" / "+state.MaxHp,body);
            Bar(new Rect(40,87,244,7),state.Hp/(float)Mathf.Max(1,state.MaxHp),Jade);
            Label(new Rect(40,105,244,20),"等级 "+state.Level+"     经验 "+state.Xp+" / "+state.XpToNext,tiny);
            Bar(new Rect(24,127,276,3),state.Xp/(float)Mathf.Max(1,state.XpToNext),Gold);
            Panel(new Rect(width/2-70,22,140,64),new Color(.7f,.76f,.72f));
            Label(new Rect(width/2-70,25,140,38),Clock(state.Now),center);
            var sub=new GUIStyle(tiny){alignment=TextAnchor.MiddleCenter};
            Label(new Rect(width/2-70,64,140,18),"秋收声场  /  10:00",sub);
            Panel(new Rect(width-236,22,212,88),Gold);
            Label(new Rect(width-220,34,182,24),"消除  "+state.Kills,body);
            float cd=Mathf.Max(0,sim.DodgeReadyAt-state.Now);
            Label(new Rect(width-220,64,182,22),cd<=0?"SPACE  闪避就绪":"闪避恢复  "+cd.ToString("F1")+"s",small);
            Bar(new Rect(width-220,94,180,3),cd<=0?1:1/(1+cd),cd<=0?Jade:Muted);
            if(view!=null)Loadout(view,width,height);
            if(state.Phase==RunPhase.LevelUp)Choices(view,commands,width,height);
            if(state.Phase==RunPhase.Paused||(state.Phase.IsTerminal()&&(!gameplayNextSettlement||sim.Outcome==null)))Menu(width,height);
            GUI.matrix=oldMatrix;
        }
        void Loadout(RunContentView view,float width,float height)
        {
            float panelWidth=Mathf.Min(740,width-48),left=(width-panelWidth)/2;
            Panel(new Rect(left,height-96,panelWidth,72),new Color(.26f,.43f,.42f));
            Label(new Rect(left+16,height-86,52,18),"武器",tiny);
            for(int i=0;i<4;i++)Slot(view.Weapons,i,new Rect(left+64+i*(panelWidth-80)/4,height-86,(panelWidth-88)/4,25),true);
            Label(new Rect(left+16,height-53,52,18),"被动",tiny);
            for(int i=0;i<6;i++)Slot(view.Passives,i,new Rect(left+64+i*(panelWidth-80)/6,height-53,(panelWidth-88)/6,21),false);
            Label(new Rect(24,height-19,width-48,17),"WASD 移动    SPACE 闪避    ESC 暂停    F6 天气    F11 全屏",tiny);
        }
        void Slot(IReadOnlyList<RunContentOwnedView> items,int i,Rect r,bool weapon)
        {
            if(items==null||i>=items.Count||string.IsNullOrEmpty(items[i].Id)){Label(r,"—",tiny);return;}
            var item=items[i];var icon=GetComponent<RC02CatalogPresenter>()?.Icon(item.Id);
            if(icon!=null){GUI.DrawTexture(new Rect(r.x,r.y,19,19),icon,ScaleMode.ScaleToFit);r.x+=24;r.width-=24;}
            Label(r,item.DisplayName+" "+item.Rank,(weapon?small:tiny));
        }
        void Choices(RunContentView view,IRunContentCommands commands,float width,float height)
        {
            RectFill(new Rect(0,0,width,height),new Color(.015f,.04f,.045f,.65f));
            var panel=ImguiScale.Fit(new Rect((width-1080)/2,(height-590)/2,1080,590));Panel(panel,Jade);
            Label(new Rect(panel.x+28,panel.y+24,700,38),"选择下一种共鸣",title);
            Label(new Rect(panel.x+28,panel.y+68,950,25),"1 / 2 / 3 或点击选择 · 以下为真实升级作用，最终效果会叠加已装备的被动。",small);
            if(view==null){Label(new Rect(panel.x+28,panel.y+120,900,40),"成长信息尚未就绪",body);return;}
            bool previous=GUI.enabled;GUI.enabled=commands!=null&&loop.Sim.State.Phase==RunPhase.LevelUp;
            int count=Mathf.Min(3,view.Choices?.Count??0);
            for(int i=0;i<count;i++)
            {
                var card=view.Choices[i];float cardWidth=(panel.width-80)/3;
                var r=new Rect(panel.x+24+i*(cardWidth+16),panel.y+110,cardWidth,350);
                RectFill(r,new Color(.075f,.13f,.14f,.98f));RectFill(new Rect(r.x,r.y,cardWidth,3),i==1?Gold:Jade);
                Label(new Rect(r.x+18,r.y+17,72,40),(i+1).ToString("00"),number);
                var icon=GetComponent<RC02CatalogPresenter>()?.Icon(card.Id);
                if(icon!=null)GUI.DrawTexture(new Rect(r.x+cardWidth-64,r.y+18,42,42),icon,ScaleMode.ScaleToFit);
                Label(new Rect(r.x+18,r.y+72,cardWidth-36,40),card.DisplayName,new GUIStyle(title){fontSize=21});
                Label(new Rect(r.x+18,r.y+115,cardWidth-36,25),"等级 "+card.CurrentRank+" → "+card.NextRank,small);
                var effectStyle=new GUIStyle(body){fontSize=14};
                var effectRect=new Rect(r.x+18,r.y+153,cardWidth-36,129);
                float textWidth=effectRect.width-18;
                float textHeight=Mathf.Max(effectRect.height,effectStyle.CalcHeight(new GUIContent(card.EffectText),textWidth));
                choiceScroll[i]=GUI.BeginScrollView(effectRect,choiceScroll[i],new Rect(0,0,textWidth,textHeight));
                Label(new Rect(0,0,textWidth,textHeight),card.EffectText,effectStyle);GUI.EndScrollView();
                if(GUI.Button(new Rect(r.x+18,r.y+298,cardWidth-102,36),"选择",button))lastResult=commands.TryChoose(view.OfferToken,card.Id)?"":"选择已失效";
                if(GUI.Button(new Rect(r.x+cardWidth-76,r.y+298,58,36),"放逐",button))lastResult=commands.TryBanish(view.OfferToken,card.Id)?"":"放逐已失效";
            }
            if(count==0)Label(new Rect(panel.x+28,panel.y+145,700,40),"当前没有可选升级",body);
            float y=panel.y+477;
            if(GUI.Button(new Rect(panel.x+24,y,150,34),"重抽  "+view.Rerolls,button))lastResult=commands.TryReroll(view.OfferToken)?"":"重抽不可用";
            if(GUI.Button(new Rect(panel.x+190,y,150,34),"跳过  "+view.Skips,button))lastResult=commands.TrySkip(view.OfferToken)?"":"跳过不可用";
            GUI.enabled=previous;
            Label(new Rect(panel.x+362,y+7,panel.width-390,25),"武器 4 槽 · 被动 6 槽   "+lastResult,small);
            if(view.Evolutions!=null)
            {
                string text="";foreach(var e in view.Evolutions)text+=(e.IsEligible?"可进化  ":"进化条件  ")+e.DisplayName+"："+e.RequirementText+"    ";
                Label(new Rect(panel.x+24,panel.y+527,panel.width-48,51),text,tiny);
            }
        }
        void Menu(float width,float height)
        {
            RectFill(new Rect(0,0,width,height),new Color(.015f,.04f,.045f,.6f));
            var r=ImguiScale.Fit(new Rect(width/2-210,height/2-150,420,300));Panel(r,Jade);
            var st=loop.Sim.State;
            Label(new Rect(r.x+30,r.y+28,360,40),st.Phase==RunPhase.Paused?"稍作休息":st.Phase==RunPhase.Victory?"声场已稳定":"本局结束",title);
            Label(new Rect(r.x+30,r.y+88,360,65),"时间 "+Clock(st.Now)+"   等级 "+st.Level+"\n消除 "+st.Kills,body);
            if(st.Phase==RunPhase.Paused&&GUI.Button(new Rect(r.x+30,r.y+170,360,42),"继续",button))loop.TogglePause();
            if(GUI.Button(new Rect(r.x+30,r.y+228,360,42),"重新开始",button))loop.Restart();
        }
        void OnDestroy()
        {
            if(buttonFill!=null)Destroy(buttonFill);if(buttonHover!=null)Destroy(buttonHover);
            if(font!=null&&font.name!="LegacyRuntime")Destroy(font);
        }
    }
}
