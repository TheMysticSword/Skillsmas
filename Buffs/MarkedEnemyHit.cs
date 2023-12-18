using MysticsRisky2Utils.BaseAssetTypes;
using RoR2;
using UnityEngine;
using MysticsRisky2Utils;
using R2API;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;

namespace Skillsmas.Buffs
{
    public class MarkedEnemyHit : BaseBuff
    {
        public override void OnLoad() {
            buffDef.name = "Skillsmas_MarkedEnemyHit";
            buffDef.canStack = false;
            buffDef.isHidden = true;
        }
    }
}
