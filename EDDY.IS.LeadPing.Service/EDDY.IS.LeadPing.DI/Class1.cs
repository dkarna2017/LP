using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDDY.IS.LeadPing.DI
{
    public class Class1
    {
        public string execute()
        {
            // Declare a Unity Container
            var unityContainer = new UnityContainer();

            // Register IGame so when dependecy is detected
            // it provides a TrivialPursuit instance
            unityContainer.RegisterType<IGame, TicTacToe>();

            // Instance a Table class object through Unity
            var table = unityContainer.Resolve<Table>();

            table.AddPlayer();
            table.AddPlayer();
            table.Play();

            return table.GameStatus();
        }
    }
}
