# jsondb 0.1 release
simple nosql database using json format from unity

simple way! for unity!

Wellcome to JsonDB of Sas.

it's made to use json easy at file system like nosql db.

depency : https://www.newtonsoft.com/json

json.net guide : https://www.newtonsoft.com/json/help/html/Introduction.htm

example >>

    string json = @"[
    {
      'Title': 'Json.NET is awesome!',
      'Author': {
        'Name': 'James Newton-King',
        'Twitter': '@JamesNK',
        'Picture': '/jamesnk.png'
      },
      'Date': '2013-01-23T19:30:00',
      'BodyHtml': '&lt;h3&gt;Title!&lt;/h3&gt;\r\n&lt;p&gt;Content!&lt;/p&gt;'
    }
    ]";

    var obj = Newtonsoft.Json.Linq.JToken.Parse (json);
    Sas.JsonDB DB = new Sas.JsonDB("test");
    DB.Put ("item1", obj);
    var root = DB.Root ();
    var title = DB.Get ("item1\\[0]\\Title");
    var title_txt = (string)title;
