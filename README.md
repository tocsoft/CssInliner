CssInliner
==========

Inlines style sheet rules. We parse and aggrigate all linked and embedded stylesheets(configurable) parses them matches them agains the dom and allies the css styles directly inline on each targeted node.

## Basic usage

```
var inlineHtmlComesoutHere = await Tocsoft.CssInliner.Processor.Process(htmlGoesHere);
```

Example
```
<html>
    <head>
        <style type="text/css">
            a#4
            {
                color: blue;
            }    
        </style>
    </head>
    <body>
        <div>
            <a id="4" href="#" ></a>
        </div>
    </body>
</html>
```

will get translated into 

```
<html>
    <head>
        <style type="text/css">
            a#4
            {
                color: blue;
            }    
        </style>
    </head>
    <body>
        <div>
            <a id="4" href="#" style "color: blue;"></a>
        </div>
    </body>
</html>
```
We leave any defined styles inline be default so that things line media queries etc will not be effected


# TODO
* Add More Tests at the moment we have tests covering some of the basics we needs to add a larger set of tests