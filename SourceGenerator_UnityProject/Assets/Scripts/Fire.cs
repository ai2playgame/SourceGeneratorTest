using AutoProperty;
using UnityEngine;


namespace MyNamespace
{
    public partial class Fire : MonoBehaviour
    {
        [AutoProperty] private int hp;
        [AutoProperty] private int mp;

        private void Func()
        {
        }
    }
}
