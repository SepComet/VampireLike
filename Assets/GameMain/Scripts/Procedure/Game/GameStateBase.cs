using System.Collections;
using System.Collections.Generic;
using GameFramework.Fsm;
using GameFramework.Procedure;
using UnityEngine;

namespace Procedure
{
    public abstract class GameStateBase
    {
        public abstract GameStateType GameStateType { get; }
        public abstract void OnInit(ProcedureGame master);
        public abstract void OnEnter(IFsm<IProcedureManager> procedureOwner);

        public abstract void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds,
            float realElapseSeconds);

        public abstract void OnLeave(IFsm<IProcedureManager> procedureOwner);
        public abstract void OnDestroy(IFsm<IProcedureManager> procedureOwner);
    }
}