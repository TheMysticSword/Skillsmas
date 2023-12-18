using MysticsRisky2Utils.BaseAssetTypes;

namespace Skillsmas.Buffs
{
    public class MarkedEnemyKill : BaseBuff
    {
        public override void OnLoad() {
            buffDef.name = "Skillsmas_MarkedEnemyKill";
            buffDef.canStack = true;
            buffDef.isHidden = true;
        }
    }
}
