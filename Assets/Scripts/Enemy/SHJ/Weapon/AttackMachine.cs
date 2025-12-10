using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackMachine : MonoBehaviour
{
    


    private Dictionary<string, CharacterMainState> stateDic = new Dictionary<string, CharacterMainState>();

    private CharacterMainState curState;
    void Start()
    {
        curState.Enter();
    }

    void Update()
    {
        
        curState.Update();
        
        curState.Transition();
    }
    private void LateUpdate()
    {
        curState.LateUpdate();
    }
    private void FixedUpdate()
    {
        curState.FixedUpdate();
    }
    public void InitState(string stateName)
    {
        curState = stateDic[stateName];
    }
    
    public void AddState(string stateName, CharacterMainState state)
    {
        state.SetStateMachine(this);
        stateDic.Add(stateName, state);
    }
   
    public void ChangeState(string stateName)
    {
        curState.Exit();
       
        curState = stateDic[stateName];

        curState.Enter();
    }

    public void InitState<T>(T stateType) where T : Enum
    {
        InitState(stateType.ToString());
    }
    public void AddState<T>(T stateType, CharacterMainState state) where T : Enum
    {
        AddState(stateType.ToString(), state);
    }
    public void ChangeState<T>(T stateType) where T : Enum
    {
        ChangeState(stateType.ToString());
    }

}


public class CharacterMainState 
{
   
    private AttackMachine stateMachine;

    public void SetStateMachine(AttackMachine stateMachine)
    {
        this.stateMachine = stateMachine;
    }
    
    protected void ChangeState(string stateName)
    {
        
        stateMachine.ChangeState(stateName);
    }
    protected void ChangeState<T>(T stateType) where T : Enum
    {
        ChangeState(stateType.ToString());
    }
    
    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void LateUpdate() { }
    public virtual void FixedUpdate() { }
    public virtual void Exit() { }
    public virtual void Transition() { }

}
