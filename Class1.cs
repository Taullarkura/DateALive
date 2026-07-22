using BaseMod;
using Battle.DiceAttackEffect;
using DateALive.Utils;
using HarmonyLib;
using Spine;
using Spine;
using Spine.Unity;
using Spine.Unity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Logger = DateALive.Utils.Logger;

namespace DateALive.Utils
{
    public class Logger
    {
        public static readonly Logger Instance = new Logger();
        public string prefix = "DateALive";
        public string Log_Path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\log.txt";
        private void Base_Log(string con,string level)
        {
            File.AppendAllText(Log_Path, $"[{level}]{con}\r\n");
        }
        public void Info(string con)
        {
            Base_Log(con, "Info");
        }
        public void Warning(string con)
        {
            Base_Log(con,"Warning");
        }
        public void Error(string con)
        {
            Base_Log(con,"Error");
        }
        public void Error(Exception ex)
        {
            Base_Log(ex.ToString(), "Error");
        }
    }
}
namespace DateALive
{

    /// <summary>
    /// HP初始化类，后面换成Initializer
    /// </summary>
    public class DateALiveInitializer:ModInitializer
    {
        public static string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public override void OnInitializeMod()
        {
            //加载特效
            Logger.Instance.Info("Start to load Spine Effect...");
            foreach (var item in Effects)
            {
                string folder = Path.Combine(path, "Spine\\" + item);
                string json = Path.Combine(folder, $"{item}.json");
                string atlas = Path.Combine(folder, $"{item}.atlas");
                string[] pngs = Directory.GetFiles(folder, "*.png");
                var json_content = new TextAsset(File.ReadAllText(json));
                List<Material> mats = new List<Material>();
                foreach (var png_path in pngs)
                {
                    mats.Add(CreateMaterialForSkel(png_path, Path.GetFileName(png_path)));
                }
                var atlasBase = SpineAtlasAsset.CreateRuntimeInstance(new TextAsset(File.ReadAllText(atlas)), mats.ToArray(), true);
                var go = SkeletonRenderer.NewSpineGameObject<SkeletonAnimation>(SkeletonDataAsset.CreateRuntimeInstance(
                        json_content,
                        atlasBase,
                        true,
                        0.01f
                    ));
                if (!SkeletonAnimationMap.ContainsKey(item))
                {
                    SkeletonAnimationMap.Add(item, go);
                    Logger.Instance.Info("Add Spine Effect:" + item);
                }
            }
            Logger.Instance.Info("End Load Spine Effects");
            Logger.Instance.Info("Init Complete");
            base.OnInitializeMod();
        }
        /// <summary>
        /// 特效列表，填文件夹名字，放Spine下面
        /// </summary>
        public static List<string> Effects= new List<string>()
        {
            "effects_10101_skillA"
        };
        public static Dictionary<string,SkeletonAnimation> SkeletonAnimationMap = new Dictionary<string, SkeletonAnimation>();
        public static Material CreateMaterialForSkel(string imagepath, string name)
        {
            try
            {
                Shader shader = Shader.Find("UI/Default");
                Texture2D texture2D = new Texture2D(2, 2);
                byte[] data = File.ReadAllBytes(imagepath);
                texture2D.LoadImage(data);
                texture2D.name = name;
                var mat = new Material(shader)
                {
                    mainTexture = texture2D
                };
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

                // 3. 关闭深度写入（透明物体通常需要）
                mat.SetInt("_ZWrite", 0);

                // 4. 强制启用 Alpha 测试（以防被裁剪）
                mat.SetFloat("_Cutoff", 0f);

                // 5. 设置渲染队列为 Transparent（确保渲染顺序正确）
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                // 6. 标记材质已修改（强制更新）
                mat.EnableKeyword("_ALPHABLEND_ON");
                return mat;
            }
            catch (Exception e)
            {
                Logger.Instance.Error(e);
            }
            return null;
        }
    }
    /// <summary>
    /// 测试用特效
    /// </summary>
    public class DiceAttackEffect_TestEffect:DiceAttackEffect
    {
        public override void Initialize(BattleUnitView self, BattleUnitView target, float destroyTime)
        {
            try
            {
                this._self = self.model;
                this._selfTransform = self.atkEffectRoot;
                this._targetTransform = target.atkEffectRoot;
                if (animation == null)
                {
                    TextAsset skeletonDataFile = new TextAsset(File.ReadAllText(Path.Combine(DateALiveInitializer.path, SpineFileName + ".json")));
                    AtlasAssetBase atlasAsset = SpineAtlasAsset.CreateRuntimeInstance(new TextAsset(File.ReadAllText(Path.Combine(DateALiveInitializer.path, SpineFileName + ".atlas"))), new Material[]
                    {
                          DateALiveInitializer.CreateMaterialForSkel(Path.Combine(DateALiveInitializer.path ,"effects_10101_skillA.png"), "effects_10101_skillA"),
                          DateALiveInitializer.CreateMaterialForSkel(Path.Combine(DateALiveInitializer.path ,"effects_10101_skillA2.png"), "effects_10101_skillA2"),
                          DateALiveInitializer.CreateMaterialForSkel(Path.Combine(DateALiveInitializer.path ,"effects_10101_skillA3.png"), "effects_10101_skillA3"),
                          DateALiveInitializer.CreateMaterialForSkel(Path.Combine(DateALiveInitializer.path ,"effects_10101_skillA4.png"), "effects_10101_skillA4")
                    }, false);
                    animation = SkeletonRenderer.NewSpineGameObject<SkeletonAnimation>(SkeletonDataAsset.CreateRuntimeInstance(skeletonDataFile, atlasAsset, false, 0.01f));
                
                }
                animation.gameObject.transform.SetParent(this._targetTransform);
                animation.gameObject.transform.position = this._targetTransform.position;
                if (self.model.direction == Direction.LEFT)
                {
                    animation.gameObject.transform.localScale = this._targetTransform.localScale;
                }
                else
                {
                    animation.gameObject.transform.localScale = new Vector3(-(this._targetTransform.localScale.x + 1f), this._targetTransform.localScale.y + 1f, this._targetTransform.localScale.z + 1f);
                }
                animation.gameObject.layer = LayerMask.NameToLayer("Effect");
                this.AniStart();
            }catch(Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }
        public void AniStart()
        {
            animation.gameObject.SetActive(true);
            TrackEntry trackEntry = animation.state.SetAnimation(0, "hit", false);
        }
        public static SkeletonAnimation animation;
        public static string SpineFileName => "effects_10101_skillA";
    }
    /// <summary>
    /// Spine特效基类
    /// </summary>
    public class SpineAttackEffect : DiceAttackEffect
    {
        public void InitAnim()
        {
            if(DateALiveInitializer.SkeletonAnimationMap.ContainsKey(SpineFileName))
            animation = DateALiveInitializer.SkeletonAnimationMap[SpineFileName];
            else
            {
                Logger.Instance.Warning($"Not Found Spine Effect:{SpineFileName},检查是否存在对应文件!");
            }
        }
        public override sealed void Initialize(BattleUnitView self, BattleUnitView target, float destroyTime)
        {
            this._self = self.model;
            this._selfTransform = self.atkEffectRoot;
            this._targetTransform = target.atkEffectRoot;
            if(AnimName==""||SpineFileName=="")
            {
                Logger.Instance.Warning("未配置动画名/特效名，特效无法加载！");
            }
            if(animation==null)
            {
                InitAnim();
            }
            animation.gameObject.transform.SetParent(this._targetTransform);
            animation.gameObject.transform.position = this._targetTransform.position;
            if (self.model.direction == Direction.LEFT)
            {
                animation.gameObject.transform.localScale = Vector3.Scale(_targetTransform.localScale, Scale);
            }
            else
            {
                animation.gameObject.transform.localScale = Vector3.Scale(new Vector3(-(this._targetTransform.localScale.x), this._targetTransform.localScale.y, this._targetTransform.localScale.z),Scale);
            }
            animation.gameObject.layer = LayerMask.NameToLayer("Effect");
            InitializeCutsom();
            AnimStart();
            base.Initialize(self, target, destroyTime);
        }
        /// <summary>
        /// 自定义加载，在正常加载配置结束，动画开始播放前执行
        /// </summary>
        public virtual void InitializeCutsom()
        {

        }
        private void AnimStart()
        {
            animation.gameObject.SetActive(true);
            TrackEntry trackEntry = animation.state.SetAnimation(0, AnimName, false);
            trackEntry.Complete += AnimEnd;
        }
        private void AnimEnd(TrackEntry trackEntry)
        {
            animation.gameObject.SetActive(false);
        }
        private SkeletonAnimation animation;
        /// <summary>
        /// 用于播放的动画名
        /// </summary>
        public virtual string AnimName => "";
        /// <summary>
        /// 动画文件名，用于检索动画对象
        /// </summary>
        public virtual string SpineFileName => "";
        /// <summary>
        /// 缩放大小，建议使用Vector3.one * 浮点数值（如*1.5f）
        /// </summary>
        public virtual Vector3 Scale => Vector3.one;
    }
}
