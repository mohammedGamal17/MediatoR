namespace MoMediatoR
{
    /// <summary>
    /// Provides extension methods for working with tasks.
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        /// Asynchronously converts the result of a given task to an object.
        /// This method awaits the provided task and returns its result as an object.
        /// </summary>
        /// <typeparam name="T">The type of the result produced by the task.</typeparam>
        /// <param name="task">The task to be converted.</param>
        /// <returns>A task that represents the asynchronous operation, 
        /// containing the result of the input task as an object.</returns>
        public static async Task<object> ConvertTaskResult<T>(Task<T> task)
        {
            return await task.ConfigureAwait(false);
        }
    }
}
