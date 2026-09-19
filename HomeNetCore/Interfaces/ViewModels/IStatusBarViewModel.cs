using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNetCore.Interfaces.ViewModels
{
    public interface IStatusBarViewModel
    {
        public record TextChanged(string NewStatus);
    }
}
