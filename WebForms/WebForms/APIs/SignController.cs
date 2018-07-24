using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace WebForms.APIs {
	public class SignController : ApiController {
		[Route("api/sign/init")]
		[HttpGet]
		public IEnumerable<string> Init() {
			return new string[] { "value1", "value2" };
		}

		// POST: api/Sign
		public void Post([FromBody]string value) {
		}

		// PUT: api/Sign/5
		public void Put(int id, [FromBody]string value) {
		}

		// DELETE: api/Sign/5
		public void Delete(int id) {
		}
	}
}
