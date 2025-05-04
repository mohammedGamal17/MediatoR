namespace MoMediatoR
{
    /// <summary>
    /// Represents configuration options for the MoMediatoR library, 
    /// allowing customization of global pipeline behaviors applied to all requests.
    /// </summary>
    public class MoMediatoROptions
    {
        private readonly List<Type> _globalPipelineBehaviors = new();

        /// <summary>
        /// A IReadOnlyList of types implementing <see cref="IPipelineBehavior{TRequest, TResponse}"/> 
        /// that will be applied globally to every request handled by the mediator.
        /// These behaviors are added in the order they appear in the list.
        /// </summary>
        public IReadOnlyList<Type> GlobalPipelineBehaviors => _globalPipelineBehaviors.AsReadOnly();

        public void RegisterBehavior(Type behaviorType)
        {
            if (behaviorType == null) throw new ArgumentNullException(nameof(behaviorType));

            bool isValid = behaviorType.IsGenericTypeDefinition
                ? behaviorType.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
                : typeof(IPipelineBehavior<,>).IsAssignableFrom(behaviorType);

            if (!isValid)
                throw new ArgumentException($"Type {behaviorType.Name} must implement IPipelineBehavior<TRequest,TResponse>");

            _globalPipelineBehaviors.Add(behaviorType);
        }
    }
}
