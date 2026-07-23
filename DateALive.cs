using BaseMod;
using Battle.DiceAttackEffect;
using DateALive.Utils;
using HarmonyLib;
using Spine;
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
            File.AppendAllText(Log_Path, $"[{DateTime.Now.ToString("HH:mm:ss.fff")}][{prefix}][{level}]{con}\r\n");
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
    /// HP初始化类
    /// </summary>
    public class DateALiveInitializer:ModInitializer
    {
        private class DebugMono:MonoBehaviour
        {
            public void OnDestroy()
            {
                Logger.Instance.Info($"go {gameObject.name} destroy!");
                
            }

        }
        public static string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static Shader MatShader { get; private set; }
        public override void OnInitializeMod()
        {
            File.Delete(Logger.Instance.Log_Path);
            MatShader = Shader.Find("UI/Default");
            Logger.Instance.Info($"MatShader 名称: {MatShader?.name ?? "NULL"}");
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
                    var mat = CreateMaterialForSkel(png_path, Path.GetFileNameWithoutExtension(png_path));
                    if (mat != null)
                        mats.Add(mat);
                    else
                        Logger.Instance.Error("Mat is null!");
                    Logger.Instance.Info("Load png:" + png_path);
                }
                var atlasBase = SpineAtlasAsset.CreateRuntimeInstance(new TextAsset(File.ReadAllText(atlas)), mats.ToArray(), true);
                Logger.Instance.Info($"AtlasAsset.materials 长度: {atlasBase.materials.Length}");
                var go = SkeletonRenderer.NewSpineGameObject<SkeletonAnimation>(SkeletonDataAsset.CreateRuntimeInstance(
                        json_content,
                        atlasBase,
                        true,
                        0.01f
                    ));
                
                
                if (!SkeletonAnimationMap.ContainsKey(item))
                {
                    if (go != null)
                    {
                        go.gameObject.AddComponent<DebugMono>();
                        var renderer = go.gameObject.GetComponent<MeshRenderer>();
                        if (renderer != null)
                        {
                            renderer.materials = atlasBase.materials;
                        }
                        else
                        {
                            Logger.Instance.Error("renderer is null");
                        }
                        GameObject.DontDestroyOnLoad(go.gameObject);
                        GameObject.DontDestroyOnLoad(go);
                        SkeletonAnimationMap.Add(item, go);


                    }
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
                Shader shader = MatShader;
                Texture2D texture2D = new Texture2D(2, 2);
                byte[] data = File.ReadAllBytes(imagepath);
                texture2D.LoadImage(data);
                texture2D.name = name;
                var mat = new Material(shader)
                {
                    mainTexture = texture2D
                };
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
    /// Spine特效基类
    /// </summary>
    public class SpineAttackEffect : DiceAttackEffect
    {
        public void InitAnim()
        {
            if (DateALiveInitializer.SkeletonAnimationMap.ContainsKey(SpineFileName))
            {
                var prototype = DateALiveInitializer.SkeletonAnimationMap[SpineFileName];
                if (prototype == null)
                {
                    Logger.Instance.Warning($"Spine prototype is null for:{SpineFileName}");
                    return;
                }
                try
                {
                    // 实例化一个新的 GameObject，避免复用同一个组件导致空引用或状态冲突
                    var go = UnityEngine.Object.Instantiate(prototype.gameObject);
                    go.name = prototype.gameObject.name + "_inst";
                    animation = go.GetComponent<SkeletonAnimation>();
                    if (animation == null)
                    {
                        Logger.Instance.Warning($"实例化后未找到 SkeletonAnimation 组件: {SpineFileName}");
                        UnityEngine.Object.Destroy(go);
                    }
                    MeshRenderer renderer = go.GetComponent<MeshRenderer>();
                    if (renderer != null && renderer.materials.Length > 0)
                    {
                        // 检查我们的材质是否真的没问题
                        foreach (var item in renderer.materials)
                        {
                            if (item == null)
                            {
                                Logger.Instance.Error("materials[0] 是 null！");
                            }
                            else
                            {
                                Logger.Instance.Info($"我们的材质 Shader: {item.shader?.name ?? "NULL"}");
                                
                            }
                        }

                    }
                    else
                    {
                        Logger.Instance.Error($"Renderer 为空或 materials 为空！materials.Count={renderer.materials.Length}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex);
                }
            }
            else
            {
                Logger.Instance.Warning($"Not Found Spine Effect:{SpineFileName},检查是否存在对应文件!");
            }
        }
        public override sealed void Initialize(BattleUnitView self, BattleUnitView target, float destroyTime)
        {
            try
            {
                this._self = self.model;
                this._selfTransform = self.atkEffectRoot;
                this._targetTransform = target.atkEffectRoot;
                if (AnimName == "" || SpineFileName == "")
                {
                    Logger.Instance.Warning("未配置动画名/特效名，特效无法加载！");
                }
                if (animation == null)
                {
                    InitAnim();
                    Logger.Instance.Info("已加载动画" + SpineFileName);
                }
                if(animation.gameObject==null)
                {
                    Logger.Instance.Info("go is null???");
                }
                animation.gameObject.transform.SetParent(this._targetTransform);
                Logger.Instance.Info("Set Parent");
                animation.gameObject.transform.position = this._targetTransform.position;
                Logger.Instance.Info("Set position");
                if (self.model.direction == Direction.LEFT)
                {
                    animation.gameObject.transform.localScale = Vector3.Scale(_targetTransform.localScale, Scale);
                }
                else
                {
                    animation.gameObject.transform.localScale = Vector3.Scale(new Vector3(-(this._targetTransform.localScale.x + 1f), this._targetTransform.localScale.y + 1f, this._targetTransform.localScale.z + 1f), Scale);
                }
                animation.gameObject.layer = LayerMask.NameToLayer("Effect");
                InitializeCutsom();
                AnimStart();
                base.Initialize(self, target, destroyTime);
            }catch(Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }
        /// <summary>
        /// 自定义加载，在正常加载配置结束，动画开始播放前执行
        /// </summary>
        public virtual void InitializeCutsom()
        {

        }
        private void AnimStart()
        {
            Logger.Instance.Info("播放动画");
            animation.gameObject.SetActive(true);
            TrackEntry trackEntry = animation.state.SetAnimation(0, AnimName, false);
            trackEntry.Complete += AnimEnd;
        }
        private void AnimEnd(TrackEntry trackEntry)
        {
            Logger.Instance.Info("动画结束");
            GameObject.Destroy(animation.gameObject);
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
