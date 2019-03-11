using System;
using System.Windows.Input;
using Atomix.Client.Wpf.Common;
using Atomix.Client.Wpf.Properties;

namespace Atomix.Client.Wpf.ViewModels
{
    public class MessageViewModel : BaseViewModel
    {
        private readonly Action _backAction;
        private readonly Action _nextAction;

        public string Title { get; }
        public string Text { get; }
        public string BackText { get; }
        public string NextText { get; }
        public bool IsBackVisible { get; }
        public bool IsNextVisible { get; }

        private ICommand _backCommand;
        public ICommand BackCommand => _backCommand ?? (_backCommand = new Command(_backAction));

        private ICommand _nextCommand;
        public ICommand NextCommand => _nextCommand ?? (_nextCommand = new Command(_nextAction));

        public MessageViewModel()
        {
        }

        public MessageViewModel(
            string title,
            string text,
            string backTitle,
            string nextTitle,
            Action backAction,
            Action nextAction)
        {
            Title = title;
            Text = text;

            BackText = backTitle;
            NextText = nextTitle;

            IsBackVisible = !string.IsNullOrEmpty(BackText);
            IsNextVisible = !string.IsNullOrEmpty(NextText);

            _backAction = backAction;
            _nextAction = nextAction;
        }

        public static MessageViewModel Error(string text)
        {
            return Error(text, goBackPages: 1);
        }

        public static MessageViewModel Error(string text, int goBackPages)
        {
            return new MessageViewModel(
                title: Resources.SvError,
                text: text,
                backTitle: Resources.SvBack,
                nextTitle: null,
                backAction: () =>
                {
                    for (var i = 0; i < goBackPages; ++i)
                        Navigation.Back();
                },
                nextAction: null);
        }

        public static MessageViewModel Success(string text, Action nextAction)
        {
            return new MessageViewModel(
                title: Resources.SvSuccess,
                text: text,
                backTitle: null,
                nextTitle: Resources.SvOk,
                backAction: null,
                nextAction: nextAction);
        }
    }
}