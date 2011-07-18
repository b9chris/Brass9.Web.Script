<%@ Page Language="C#" Inherits="System.Web.Mvc.ViewPage<dynamic>" %>

<!DOCTYPE html>

<html>
<head runat="server">
<title>Index</title>

<%
Html.IncludeCss("component1");
%>

<style>
body {
	font-family: Verdana;
}
</style>
</head>
<body>

<p>Testing testing 1 2 3...</p>

<script>
function init() {
	var c = new Component();
	c.say('On-page script loaded properly after component scripts');
}
</script>

<%
// An example of running on-page JS after all async scripts have loaded in order of dependency chain.
Html.RunJsAfter("init()", "pagescript1");

// If you had no on-page JS but still wanted pagescript1 and its dependency chain to load, all you need
// to call is:
//Html.IncludeScript("pagescript1");

// If you had not globally-defined pagescript1 and instead needed to declare it here on the page - for
// example, if its dependencies were not global but rather varied between requests (or you just rather
// not declare it globally), the syntax would look like this:
//Html.IncludeScript("pagescript1", "pagescript1.js", "pagescript1.js", "component2");
// This both declares and includes pagescript1 into this page (with its dependency chain going back from
// component2

// This defaults to ScriptsRenderMode.Simple in DEBUG mode, and ScriptsRenderMode.Async in Release mode.
// Forced to Async here to demonstrate LabJs usage in typical first testing.
Scripts.Current.RenderMode = ScriptsRenderMode.Async;

// This should be called at the bottom of the body tag. Typically used in Master pages so you can forget
// about it on most pages on a site.
Html.RenderScripts();
%>
</body>
</html>
