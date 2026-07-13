using Owin;
using CrystalQuartz.Owin;
using Quartz;
using System.Linq;
using Quartz.Impl;
// 👈 Afegim el namespace on viu la classe d'opcions que demana el compilador
using CrystalQuartz.Application;

namespace MultirIntegraModulab
{
    public class Startup
    {
        public static IScheduler Scheduler
        {
            get
            {
                try
                {
                    var factory = new StdSchedulerFactory();
                    var schedulers = factory.GetAllSchedulers().GetAwaiter().GetResult();
                    return schedulers.FirstOrDefault();
                }
                catch
                {
                    return null;
                }
            }
        }

        public void Configuration(IAppBuilder app)
        {
            // Instanciem l'objecte afegint la barra inicial '/' a la ruta
            var options = new CrystalQuartzOptions
            {
                Path = "/quartz" // 👈 CRÍTIC: Abans deia "quartz" sense la barra inicial
            };

            // Passem les opcions corregides
            app.UseCrystalQuartz(() => Scheduler, options);
        }
    }
}