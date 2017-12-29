var realm;
var clientId;
var scopeSeparator;
var additionalQueryStringParams;
var clientSecret;

function initOAuth(e) {
    e.tokenName = 'code';
    clientId = e.clientId;
    scopeSeparator = e.scopeSeparator;
    additionalQueryStringParams = e.additionalQueryStringParams;
    clientSecret = e.clientSecret;
    realm = "123";

    window.swaggerUiAuth = window.swaggerUiAuth || {};
    window.swaggerUiAuth.tokenName = 'code';
    window.swaggerUiAuth.tokenUrl = window.swaggerUi.api.authSchemes.oauth2.tokenUrl;
    if (!window.isOpenReplaced) {
        window.open = function (open) {
            return function (url) {
                //url = url.replace('response_type=token', 'response_type=id_token');
                console.log(url);
                return open.call(window, url);
            };
        }(window.open);
        window.isOpenReplaced = true;
    }
}
window.processOAuthCode = function (e) {
    var o = e.state
        , i = window.location
        , n = location.pathname.substring(0, location.pathname.lastIndexOf("/"))
        , a = i.protocol + "//" + i.host + n + "/o2c.html"
        , t = window.oAuthRedirectUrl || a
        , p = {
            client_id: clientId,
            code: e.code,
            grant_type: "authorization_code",
            redirect_uri: t
        };
    clientSecret && (p.client_secret = clientSecret),
        $.ajax({
            url: window.swaggerUiAuth.tokenUrl,
            type: "POST",
            data: p,
            success: function (e, i, n) {
                onOAuthComplete(e, o)
            },
            error: function (e, o, i) {
                onOAuthComplete("")
            }
        })
}
,
window.onOAuthComplete = function (e, o) {
    if (e)
        if (e.error) {
            var i = $("input[type=checkbox],.secured");
            i.each(function (e) {
                i[e].checked = !1
            }),
                alert(e.error)
        } else {
            var n = e[window.swaggerUiAuth.tokenName];
            if (o || (o = e.state),
                n) {
                var a = null;
                $.each($(".auth .api-ic .api_information_panel"), function (e, o) {
                    var i = o;
                    if (i && i.childNodes) {
                        var n = [];
                        $.each(i.childNodes, function (e, o) {
                            var i = o.innerHTML;
                            i && n.push(i)
                        });
                        for (var t = [], p = 0; p < n.length; p++) {
                            var r = n[p];
                            window.enabledScopes && window.enabledScopes.indexOf(r) == -1 && t.push(r)
                        }
                        t.length > 0 ? (a = o.parentNode.parentNode,
                            $(a.parentNode).find(".api-ic.ic-on").addClass("ic-off"),
                            $(a.parentNode).find(".api-ic.ic-on").removeClass("ic-on"),
                            $(a).find(".api-ic").addClass("ic-warning"),
                            $(a).find(".api-ic").removeClass("ic-error")) : (a = o.parentNode.parentNode,
                                $(a.parentNode).find(".api-ic.ic-off").addClass("ic-on"),
                                $(a.parentNode).find(".api-ic.ic-off").removeClass("ic-off"),
                                $(a).find(".api-ic").addClass("ic-info"),
                                $(a).find(".api-ic").removeClass("ic-warning"),
                                $(a).find(".api-ic").removeClass("ic-error"))
                    }
                }),
                    "undefined" != typeof window.swaggerUi && (window.swaggerUi.api.clientAuthorizations.add(window.swaggerUiAuth.OAuthSchemeKey, new SwaggerClient.ApiKeyAuthorization("Authorization", "Bearer " + n, "header")),
                        window.swaggerUi.load())
            }
        }
};