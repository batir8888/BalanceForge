using System.Collections.Generic;

namespace BalanceForge.Services
{
    public class UndoRedoService
    {
        private Stack<ICommand> undoStack = new Stack<ICommand>();
        private Stack<ICommand> redoStack = new Stack<ICommand>();
        
        public void ExecuteCommand(ICommand command)
        {
            command.Execute();
            undoStack.Push(command);
            redoStack.Clear();
        }
        
        public void Undo()
        {
            if (CanUndo())
            {
                var command = undoStack.Pop();
                command.Undo();
                redoStack.Push(command);
            }
        }
        
        public void Redo()
        {
            if (CanRedo())
            {
                var command = redoStack.Pop();
                command.Execute();
                undoStack.Push(command);
            }
        }
        
        public void Clear()
        {
            undoStack.Clear();
            redoStack.Clear();
        }
        
        public bool CanUndo() => undoStack.Count > 0;
        public bool CanRedo() => redoStack.Count > 0;
    }
}