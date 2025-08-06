using Infrastructure.StateMachines.States.Interfaces;

namespace Infrastructure.StateMachines
{
    public interface IStateMachine
    {
        void Enter<TState>() where TState : class, IState;
        void Enter<TState, TPayload>(TPayload payload) where TState : class, IPayloadedState<TPayload>;
        void Update(float deltaTime);
    }
}