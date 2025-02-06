using System.Windows.Input;

namespace epub2txt
{
	internal class ActionCommand : ICommand
	{
		private readonly Action<Object?> mAction;

#pragma warning disable CS0067
		public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067

		public ActionCommand(Action<Object?> action) => mAction = action;
		public bool CanExecute(Object? parameter) => true;
		public void Execute(Object? parameter) => mAction(parameter);
	}
}
