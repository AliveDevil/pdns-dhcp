namespace System.Threading.Tasks;

public static class AsyncWaitHandle
{
	public static Task WaitOneAsync(this WaitHandle waitHandle, CancellationToken cancellationToken = default)
	{
		WaitOneAsyncState state = new(waitHandle, cancellationToken);
		return state.WaitOneAsync();
	}

	private class WaitOneAsyncState : IDisposable
	{
		private readonly CancellationTokenRegistration _cancellationTokenRegistration;
		private readonly RegisteredWaitHandle _registeredWaitHandle;
		private readonly TaskCompletionSource _tcs;
		private bool _disposed;

		public WaitOneAsyncState(WaitHandle waitHandle, CancellationToken cancellationToken)
		{
			_tcs = new();
			_cancellationTokenRegistration = cancellationToken.Register((state, token) => ((WaitOneAsyncState)state!).Canceled(token), this);
			_registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(waitHandle, (state, timeout) => ((WaitOneAsyncState)state!).Signaled(timeout), this, Timeout.Infinite, true);
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_registeredWaitHandle.Unregister(default);
			_cancellationTokenRegistration.Dispose();

			_disposed = true;
		}

		public Task WaitOneAsync()
		{
			const TaskContinuationOptions options = TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously;
			return _tcs.Task.ContinueWith((upstream, state) => ((WaitOneAsyncState)state!).Continuation(upstream), this, options).Unwrap();
		}

		private void Canceled(CancellationToken token)
		{
			_tcs.SetCanceled(token);
		}

		private Task Continuation(Task task)
		{
			Dispose();
			return task;
		}

		private void Signaled(bool timeout)
		{
			if (timeout)
			{
				Canceled(CancellationToken.None);
			}
			else
			{
				_tcs.SetResult();
			}
		}
	}
}
