<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width" />
    <title>@ViewData("Title")</title>
    @RenderSection("scripts", required:=False)
</head>
<body>
    @RenderBody()
</body>
</html>