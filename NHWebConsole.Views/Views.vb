Imports MiniMVC

Public Module Views
    Public Function OpenSearch(ByVal url As String) As XElement
        Return _
            <OpenSearchDescription xmlns="http://a9.com/-/spec/opensearch/1.1/">
                <ShortName>NHWebConsole HQL</ShortName>
                <Description>NHibernate HQL search</Description>
                <Url type="text/html"
                    template=<%= url + "/index.ashx?q={searchTerms}&MaxResults=10&FirstResult={startIndex?}" %>
                />
                <Url type="application/rss+xml"
                    template=<%= url + "/index.ashx?q={searchTerms}&MaxResults=10&FirstResult={startIndex?}&contentType=application%2frss%2bxml&output=RSS" %>
                />
                <Query role="example" searchTerms="from Object"/>
            </OpenSearchDescription>
    End Function

    Public Function RowDescription(ByVal e As Row) As XElement
        Return _
            <ul>
                <%= From v In e
                    Select
                    <li>
                        <b><%= v.Key %></b>:
                        <%= If(v.Value.Length > 0,
                            v.Value,
                            <x><i>NULL</i></x>.Nodes.ToArray) %>
                    </li> %>
            </ul>
    End Function

    Public Function RSS(ByVal model As Context) As XElement
        Return _
<rss version="2.0">
    <channel>
        <title><%= model.Query %></title>
        <description><%= model.Query %></description>
        <link><%= model.Url %>?q=<%= model.Query %><%= If(model.MaxResults.HasValue, "&MaxResults=" & model.MaxResults, "") %><%= If(model.FirstResult.HasValue, "&FirstResult=" & model.FirstResult, "") %></link>
        <pubDate><%= DateTime.Now.ToString("R") %></pubDate>
        <%= If(model.[Error] IsNot Nothing,
            <item>
                <title>NHibernate Exception</title>
                <description><%= model.[Error] %></description>
            </item>,
            Nothing) %>
        <%= From row In model.Results.Select(Function(r, i) New With {.e = r, .i = i})
            Select
            <item>
                <title><%= row.i %></title>
                <description>
                    <%= RowDescription(row.e).ToString %>
                </description>
            </item> %>
    </channel>
</rss>
    End Function

    Public Function Styles() As XElement
        Return _
            <style type="text/css">
		    ul {
		        list-style-type: none;
		        padding: 0px;
		    }
		    li {
		        float:left;
		        padding: 3px;
		        padding-right: 5px;
		        clear: both;
		    }
		    ul li ul li {
		        clear: none;
		    }
		    .error {
		        color: Red;
		    }

			li.odd {
				background-color: #eeeeee;
			}

			li.even {
				background-color: #ffffff;
			}

			textarea  {
			    float: left;
			}

			.entitylist {
			    overflow: auto;
			    height: 10em;
			}
			.pager {
				margin-top: 10px;
			}
			.pager a {
				text-decoration: none;
				padding: 2px 5px;
				border: 1px solid #888888;
			}
            .pagination a:hover
            {
	            background-color: #FFFFCC;
            }

            .ui-widget 
            {
            	font-size: 90%;
            }
            .ui-autocomplete  
            {
            	height: 200px; 
            	overflow-y: scroll; 
            	overflow-x: hidden;
            }
            </style>
    End Function

    Public Function QueryForm(ByVal model As Context) As XElement
        Return _
                <form method="get" action=<%= model.Url %>>
                    <textarea id="q" name="q" cols="80" rows="10" accesskey="q"><%= If(model.Query, "") %></textarea>
                    <div class="entitylist">
                        <%= From e In model.AllEntities
                            Select
                            <span>
                                <a href=<%= e.Value %>><%= e.Key %></a>
                                <br/>
                            </span> %>
                        <span></span>
                    </div>
                    <br style="clear:both"/>
	                Max results: <input type="text" name="MaxResults" value=<%= model.MaxResults %> size="2"/><br/>
	                First result: <input type="text" name="FirstResult" value=<%= model.FirstResult %> size="2"/><br/>
                    <select name="type" id="queryType">
                        <%= If(model.QueryType = QueryType.HQL,
                            <x>
                                <option value="HQL" selected="selected">HQL</option>
                                <option value="SQL">SQL</option>
                            </x>,
                            <x>
                                <option value="HQL">HQL</option>
                                <option value="SQL" selected="selected">SQL</option>
                            </x>).Nodes %>
                    </select>
                    <input type="submit" value="Run" accesskey="x"/>
                </form>

    End Function

    Public Function Pager(ByVal firstPageUrl As String, ByVal prevPageUrl As String, ByVal nextPageUrl As String) As XElement
        Return _
            <div class="pager">
                <%= If(firstPageUrl IsNot Nothing,
                    <a href=<%= firstPageUrl %> accesskey="1"><%= X.laquo %> First</a>,
                    Nothing) %>

                <%= X.nbsp %>

                <%= If(prevPageUrl IsNot Nothing,
                    <a href=<%= prevPageUrl %> accesskey=","><%= X.lsaquo %> Prev</a>,
                    Nothing) %>

                <%= X.nbsp %>

                <%= If(nextPageUrl IsNot Nothing,
                    <a href=<%= nextPageUrl %> accesskey=".">Next <%= X.rsaquo %></a>,
                    Nothing) %>

            </div>

    End Function

    Public Function Index(ByVal model As Context) As XElement
        Return _
<html>
    <head>
        <title>NHibernate console</title>
        <%= Styles() %>
        <%= If(model.RssUrl IsNot Nothing,
            <link rel="alternate" type="application/rss+xml" title="RSS" href=<%= model.RssUrl %>/>,
            Nothing) %>
        <link rel="search" type="application/opensearchdescription+xml" href="openSearch.ashx" title="HQL search"/>
        <link rel="Stylesheet" href="http://ajax.googleapis.com/ajax/libs/jqueryui/1.8.5/themes/ui-lightness/jquery-ui.css" type="text/css"/>
        <script src="http://ajax.googleapis.com/ajax/libs/jquery/1.4.3/jquery.min.js" type="text/javascript"></script>
        <script src="http://ajax.googleapis.com/ajax/libs/jqueryui/1.8.5/jquery-ui.min.js" type="text/javascript"></script>
        <script type="text/javascript">
			(function(_) {
			  _.fn.setCursorPosition = function(pos) {
			    if (_(this).get(0).setSelectionRange) {
			      _(this).get(0).setSelectionRange(pos, pos);
			    } else if (_(this).get(0).createTextRange) {
			      var range = _(this).get(0).createTextRange();
			      range.collapse(true);
			      range.moveEnd('character', pos);
			      range.moveStart('character', pos);
			      range.select();
			    }
			  }
			})(jQuery);
		</script>
    </head>
    <body>
        <%= QueryForm(model) %>
        <script type="text/javascript">
            //<![CDATA[
	        document.forms[0].onsubmit = function() {
	            var q = document.getElementsByName('q')[0].value;
	            if (/ *(insert|update|delete)/i.test(q) && this.method == 'get') {
	                this.method = 'post';
				}
	        }
            //]]>
        </script>

        <%= If(model.[Error] IsNot Nothing,
            <pre class="error"><%= model.[Error] %></pre>,
            Nothing) %>

        <%= If(model.Results IsNot Nothing,
            <x>
                <%= model.Results.Count %> results<br/>
                <ul>
                    <%= From row In model.Results.Select(Function(r, i) New With {.e = r, .i = i})
                        Select
                        <li class=<%= If(row.i Mod 2 = 0, "even", "odd") %>>
                            <%= RowDescription(row.e) %>
                        </li>
                    %>
                </ul>
            </x>,
            <x></x>).Nodes %>
        <br style="clear:both"/>
        <%= Pager(model.FirstPageUrl, model.PrevPageUrl, model.NextPageUrl) %>

        <script type="text/javascript">
        //<![CDATA[
            (function(_) {
                function getPosition(textarea) {
                    var a = document.getElementById(textarea);
                    a.focus();
                    return a.selectionStart;
                }

		        _('#q').autocomplete({
                    source: function(request, response) {
                        _.getJSON('suggestion.ashx', {
                            q: request.term,
                            p: getPosition('q'),
                        }, function(data) {
                            response(data.suggestions);
                        });
                    },
                    select: function(event, ui) {
                        _('#q').get(0).value += ui.item.value;
                        return false;
                    },
                    focus: function(event, ui) {
                        return false;
                    }
                }).keydown(function(e) {
                    if (e.keyCode == 37 || e.keyCode == 39)
                        _('#q').autocomplete('close');
                });

	            if (document.getElementById('queryType').value != 'HQL')
		            _('#q').autocomplete("disable");

	            _('#queryType').change(function() {
                    _('#q').autocomplete(this.value == 'HQL' ? "enable" : "disable");
	            });
            })(jQuery);
        //]]>
        </script>
    </body>
</html>
    End Function

    Public Function Link(ByVal url As String, ByVal text As String) As XElement
        Return <a href=<%= url %>><%= text %></a>
    End Function

    Public Function Img(ByVal src As String, Optional ByVal alt As String = "") As XElement
        Return <img src=<%= src %> alt=<%= alt %>/>
    End Function
End Module
