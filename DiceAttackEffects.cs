using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DateALive.DiceAttackEffects
{
    /// <summary>
    /// 测试用特效
    /// </summary>
    public class DiceAttackEffect_TestEffect : SpineAttackEffect
    {
        public override Vector3 Scale => Vector3.one;
        public override string AnimName => "hit";
        public override string SpineFileName => "effects_10101_skillA";
    }
}
