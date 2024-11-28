using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ihatecs.Core
{
    public class NavigationService : ObservableObject
    {
        private object _currentView;
        public object CurrentView
        {
            get { return _currentView; }
            set
            {
                _currentView = value;
                OnProperyChanged();
            }
        }

        public void NavigateTo(object viewModel)
        {
            CurrentView = viewModel;
        }
    }

}
