namespace DynamicFlow.Domain
{
    public delegate ValueTask StateMoveHandler<TState>(TState oldState, TState newState);

    public sealed class StateTable<TState>
        : Dictionary<TState, Dictionary<TState, List<StateMoveHandler<TState>>>>
        where TState : notnull
    {
        public void Add(TState oldState, TState newState, StateMoveHandler<TState> handler)
        {
            if (!ContainsKey(oldState)) Add(oldState, []);
            var oldStateGroup = base[oldState];
            if (!oldStateGroup.ContainsKey(newState)) oldStateGroup.Add(newState, []);
            var newStateList = oldStateGroup[newState];
            newStateList.Add(handler);
        }

        private static readonly IReadOnlyList<StateMoveHandler<TState>> EmptyList = new List<StateMoveHandler<TState>>();

        public IReadOnlyList<StateMoveHandler<TState>> this[TState oldState, TState newState]
        {
            get
            {
                if (!ContainsKey(oldState)) return EmptyList;
                if (!base[oldState].TryGetValue(newState, out List<StateMoveHandler<TState>>? value)) return EmptyList;
                return value;
            }
        }

        public async ValueTask MoveState(TState oldState, TState newState, CancellationToken token)
        {
            var states = this[oldState, newState];
            foreach (var state in states)
            {
                if (token.IsCancellationRequested) return;

                await state(oldState, newState);
            }
        }
    }
}
