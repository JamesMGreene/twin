using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Automation;
using Twin.Generic;
using Twin.Model;
using Twin.Proxy;

namespace Twin.View {
    class Windows {
        public static object Close(ElementRequest request) {
            request.Target.Close();
            return null;
        }
    }
}
