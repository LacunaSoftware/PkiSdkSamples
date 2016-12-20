using Lacuna.Pki;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApi.Models {

	public class ValidationErrorModel {

		public ValidationResultsModel ValidationResults { get; set; }

		public ValidationErrorModel(ValidationResults vr) {
			ValidationResults = new ValidationResultsModel(vr);
		}
	}

	public class ValidationResultsModel {

		public List<ValidationItemModel> PassedChecks { get; set; }
		public List<ValidationItemModel> Errors { get; set; }
		public List<ValidationItemModel> Warnings { get; set; }

		public ValidationResultsModel(ValidationResults vr) {
			PassedChecks = vr.PassedChecks.ConvertAll(i => new ValidationItemModel(i));
			Warnings = vr.Warnings.ConvertAll(i => new ValidationItemModel(i));
			Errors = vr.Errors.ConvertAll(i => new ValidationItemModel(i));
		}
	}

	public class ValidationItemModel {

		public string Type { get; set; }

		public string Message { get; set; }

		public string Detail { get; set; }

		public ValidationResultsModel InnerValidationResults { get; set; }

		public ValidationItemModel(ValidationItem vi) {
			Type = vi.Type.ToString();
			Message = vi.Message;
			Detail = vi.Detail;
			if (vi.InnerValidationResults != null) {
				InnerValidationResults = new ValidationResultsModel(vi.InnerValidationResults);
			}
		}
	}
}