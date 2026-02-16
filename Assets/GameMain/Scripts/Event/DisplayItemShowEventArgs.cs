using GameFramework;
using GameFramework.Event;
using UnityEngine;

namespace CustomEvent
{
    public class DisplayItemShowEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(DisplayItemShowEventArgs).GetHashCode();

        public override int Id => EventId;

        public int Index { get; private set; }

        public bool IsWeapon { get; private set; }

        public Vector3 TargetPos { get; private set; }

        public DisplayItemShowEventArgs()
        {
            Index = -1;
            IsWeapon = false;
        }

        public static DisplayItemShowEventArgs Create(int index, bool isWeapon, Vector3 targetPos)
        {
            var args = ReferencePool.Acquire<DisplayItemShowEventArgs>();
            args.Index = index;
            args.IsWeapon = isWeapon;
            args.TargetPos = targetPos;
            return args;
        }

        public override void Clear()
        {
            Index = -1;
            IsWeapon = false;
            TargetPos = Vector3.zero;
        }
    }
}