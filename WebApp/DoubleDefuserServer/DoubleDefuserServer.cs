using System;
using System.Collections.Generic;
using RT.Json;
using RT.Servers;

namespace DoubleDefuserServer
{
    public class DoubleDefuserServer : RT.PropellerApi.PropellerModuleBase<DoubleDefuserServerSettings>
    {
        public override string Name => "Double Defuser Server Backend";

        private readonly List<(NameValuesCollection<string> nvc, DateTime time)> _stored = new List<(NameValuesCollection<string> nvc, DateTime time)>();

        public override HttpResponse Handle(HttpRequest req)
        {
#if DEBUG
            if(req.Method == HttpMethod.Get && req.Url.Path != "/" && req.Url.Path != "/img/Double Defuser/")
                return new FileSystemHandler("C:/Users/benja/Desktop/ktanedoubledefuser/Manual/").Handle(req);
#endif

            if(req.Method == HttpMethod.Get)
                return HttpResponse.Json(Json);

            if(req.Post["DeviceHashcode"].Value == null)
                return HttpResponse.PlainText("", HttpStatusCode._400_BadRequest);

            _stored.RemoveAll(tup => tup.nvc["DeviceHashcode"].Value == req.Post["DeviceHashcode"].Value);
            _stored.RemoveAll(tup => (DateTime.Now - tup.time).TotalSeconds > Settings.Timeout);
            _stored.Add((req.Post, DateTime.Now));

            return HttpResponse.PlainText("");
        }

        private JsonValue Json => _stored.ToJsonList(tup => tup.nvc.ToJsonDict(kvp => kvp.Key, kvp => kvp.Value.Value));
    }
}
