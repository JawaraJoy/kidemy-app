using UnityEngine;

namespace Rush
{
    public class UpdateBankAgent : MonoBehaviour
    {
        public void StopAllTicks(bool set)
        {
            UpdateBank.Instance.SetActiveAllTicks(set);
        }
    }
}
