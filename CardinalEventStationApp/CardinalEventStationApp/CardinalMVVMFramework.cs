using Autofac;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace CardinalEventStationApp
{
    /// <summary>
    /// Cardinal Xamarin MVVM Framework
    /// -- Keep namespace same as default application
    /// -- Requires App.Container (AutoFac v4.8.1) to be defined in App.xaml.cs
    /// -- Requires AutoFacContainerBuilder.cs to register View/ViewModel/Services
    /// </summary>


    /// <summary>
    /// Base View Model
    /// </summary>
    public abstract class ViewModelBase : ExtendedBindableObject
    {
        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                _isBusy = value;
                RaisePropertyChanged(() => IsBusy);
            }
        }

        private object _initializeParameter { get; set; }
        public object InitializeParameter
        {
            get { return _initializeParameter; }
            set
            {
                _initializeParameter = value;
                RaisePropertyChanged(() => InitializeParameter);
            }
        }

        public ViewModelBase() { }

        public virtual void Initialize(object param)
        {
            InitializeParameter = param;
        }

        public abstract Task OnAppearingAsync();
    }

    /// <summary>
    /// Used by ViewModelBase
    /// </summary>
    public abstract class ExtendedBindableObject : BindableObject
    {
        public void RaisePropertyChanged<T>(Expression<Func<T>> property)
        {
            var name = GetMemberInfo(property).Name;
            OnPropertyChanged(name);
        }

        private MemberInfo GetMemberInfo(Expression expression)
        {
            MemberExpression operand;
            LambdaExpression lambdaExpression = (LambdaExpression)expression;
            if (lambdaExpression.Body as UnaryExpression != null)
            {
                UnaryExpression body = (UnaryExpression)lambdaExpression.Body;
                operand = (MemberExpression)body.Operand;
            }
            else
            {
                operand = (MemberExpression)lambdaExpression.Body;
            }
            return operand.Member;
        }
    }

    /// <summary>
    /// ContentPage View Base
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ViewPageBase<T> : ContentPage where T : ViewModelBase
    {
        private readonly T _viewModel;
        public T ViewModel
        {
            get { return _viewModel; }
        }

        public ViewPageBase()
        {
            using (var scope = App.Container.BeginLifetimeScope())
            {
                _viewModel = App.Container.Resolve<T>();
            }
            BindingContext = _viewModel;
            AttachEventHandlers();
        }

        //public async Task InitializeViewModelAsync()
        //{
        //    await (BindingContext as ViewModelBase).InitializeAsync();
        //}

        private void AttachEventHandlers()
        {
            Appearing += async (sender, e) =>
            {
                if (BindingContext is ViewModelBase viewModelBase)
                {
                    await viewModelBase.OnAppearingAsync();
                }
            };

            //Disappearing += (sender, e) =>
            //{
            //    if (BindingContext is ViewModelBase viewModelBase)
            //    {
            //        viewModelBase.OnDisappearing();
            //    }
            //};
        }
    }

    /// <summary>
    /// ContentView Base
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ViewContentBase<T> : ContentView where T : ViewModelBase
    {
        private readonly T _viewModel;
        public T ViewModel
        {
            get { return _viewModel; }
        }

        public ViewContentBase()
        {
            using (var scope = App.Container.BeginLifetimeScope())
            {
                _viewModel = App.Container.Resolve<T>();
            }
            BindingContext = _viewModel;
        }
    }
}
