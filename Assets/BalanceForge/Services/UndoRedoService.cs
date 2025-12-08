using System.Collections.Generic;

namespace BalanceForge.Services
{
    /// <summary>
    /// Сервис управления операциями Undo/Redo для таблицы баланса.
    /// Использует паттерн Command для инкапсуляции действий и поддержки отката/повтора операций.
    /// Поддерживает два стека: для отмены и для повтора команд.
    /// </summary>
    public class UndoRedoService
    {
        /// <summary>
        /// Стек команд для операции Undo.
        /// Содержит все выполненные команды в обратном порядке.
        /// </summary>
        private Stack<ICommand> undoStack = new Stack<ICommand>();
        
        /// <summary>
        /// Стек команд для операции Redo.
        /// Содержит все отмененные команды, готовые к повторному выполнению.
        /// </summary>
        private Stack<ICommand> redoStack = new Stack<ICommand>();
        
        /// <summary>
        /// Выполняет команду и добавляет её в стек Undo.
        /// Автоматически очищает стек Redo так как новая команда нарушает историю повторов.
        /// </summary>
        /// <param name="command">Команда для выполнения. Должна реализовывать интерфейс ICommand.</param>
        public void ExecuteCommand(ICommand command)
        {
            command.Execute();
            undoStack.Push(command);
            redoStack.Clear();
        }
        
        /// <summary>
        /// Отменяет последнюю выполненную команду если она есть.
        /// Перемещает команду из стека Undo в стек Redo для возможности повтора.
        /// Проверяет возможность отмены перед выполнением через CanUndo.
        /// </summary>
        public void Undo()
        {
            if (CanUndo())
            {
                var command = undoStack.Pop();
                command.Undo();
                redoStack.Push(command);
            }
        }
        
        /// <summary>
        /// Повторно выполняет последнюю отмененную команду если она есть.
        /// Перемещает команду из стека Redo в стек Undo.
        /// Проверяет возможность повтора перед выполнением через CanRedo.
        /// </summary>
        public void Redo()
        {
            if (CanRedo())
            {
                var command = redoStack.Pop();
                command.Execute();
                undoStack.Push(command);
            }
        }
        
        /// <summary>
        /// Очищает обе стеки отмены и повтора.
        /// Вызывается при загрузке новой таблицы для сброса истории операций.
        /// </summary>
        public void Clear()
        {
            undoStack.Clear();
            redoStack.Clear();
        }
        
        /// <summary>
        /// Проверяет можно ли выполнить операцию Undo.
        /// </summary>
        /// <returns>true если стек Undo содержит хотя бы одну команду, иначе false.</returns>
        public bool CanUndo() => undoStack.Count > 0;
        
        /// <summary>
        /// Проверяет можно ли выполнить операцию Redo.
        /// </summary>
        /// <returns>true если стек Redo содержит хотя бы одну команду, иначе false.</returns>
        public bool CanRedo() => redoStack.Count > 0;
    }
}