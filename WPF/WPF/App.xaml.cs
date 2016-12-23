using Lacuna.Pki;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SampleWpfApp {

	public partial class App : Application {

		public static PredefinedTrustArbitrators TrustArbitrator { get; set; }

		protected override void OnActivated(EventArgs e) {

			base.OnActivated(e);

			Lacuna.Pki.NLogConnector.NLogLogger.Configure();

			// -----------------------------------------------------------------------------------------------------------
			// SET YOUR BINARY LICENSE BELOW AND UNCOMMENT THE LINE
			//
			//PkiConfig.LoadLicense(Convert.FromBase64String("PASTE YOUR BASE64-ENCODED BINARY LICENSE HERE"));
			//                                                ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
			// -----------------------------------------------------------------------------------------------------------

			/*
				Notice: as explained in http://pki.lacunasoftware.com/Help/html/3deeb7ad-961e-4890-ab7a-893b16667689.htm#Programmatically ,
				when developing desktop applications, we ask that the license be embedded in the source code (as shown above) instead of,
				for instance, storing it on the App.config file. This has the purpose of hiding the license from any ill-intentioned users.
			 */
		}

		public static ITrustArbitrator GetTrustArbitrator() {

			switch (TrustArbitrator) {

				case PredefinedTrustArbitrators.Any:
					return new TrustAnyRootArbitrator();

				case PredefinedTrustArbitrators.PkiBrazil:
					return TrustArbitrators.PkiBrazil;

				case PredefinedTrustArbitrators.Windows:
					return TrustArbitrators.Windows;

				default:
					throw new NotSupportedException("Unknown trust arbitrator: " + TrustArbitrator);

			}
		}

	}

}
