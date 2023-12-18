using RoR2;

namespace Skillsmas
{
    public static class ExtensionMethods
    {
        public static void LoadConfiguration(this EntityStates.EntityState entityState, System.Type type)
        {
            System.Action<object> action;
            if (EntityStateCatalog.instanceFieldInitializers.TryGetValue(type, out action))
            {
                action(entityState);
            }
        }
    }
}