using UnityEngine;

namespace Rush
{
    public abstract class UIView : View
    {
        protected virtual void ConfigInternal(UISettingConfig config)
        {
            config.Config(this);
        }
        /*public void Config(UISettingConfig config)
        {
            ConfigInternal
        }*/
    }
}
