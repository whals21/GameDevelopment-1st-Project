using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBossAttack
{
    void Initialize(BossController boss);
    bool CanExecute();
    void Tick();              // ¡Ú ÀÌ°Å
    void Execute();
    void Exit();
    bool IsFinished { get; }
}
