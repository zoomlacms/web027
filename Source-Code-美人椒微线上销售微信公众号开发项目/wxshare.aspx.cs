using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;
using ZoomLa.Common;
using System.Xml;
using ZoomLa.BLL;
using ZoomLa.Model;
using Newtonsoft.Json;
using System.Web.Security;
using ZoomLa.Components;
using ZoomLa.Model.Plat;
using ZoomLa.BLL.Plat;
using Newtonsoft.Json.Linq;
using ZoomLa.BLL.WxPayAPI;
using ZoomLa.Model.User;
using ZoomLa.BLL.User.Addon;
using ZoomLa.SQLDAL;

public partial class wxshare : System.Web.UI.Page
{
    public string timestr = "";
    public string paySign = "";
    public string nonceStr = "Wm3WZYTPz0wzccnW";
    B_WX_APPID appBll = new B_WX_APPID();
    public M_WX_APPID appMod = new M_WX_APPID();
    M_UserAPP uappMod = new M_UserAPP();
    B_UserAPP uappBll = new B_UserAPP();
    public B_User buser = new B_User();
	public M_WX_User wuserMod = new M_WX_User(); 

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            M_UserInfo mu = buser.GetLogin();
			int appid = DataConverter.CLng(Request.QueryString["appid"]);
            if (mu != null && mu.UserID > 0)
            {
                try
                {
                    TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
                    timestr = Convert.ToInt64(ts.TotalSeconds).ToString();
                    if (appid == 0)
                        appid = 1;
                    WxAPI wxapi = WxAPI.Code_Get(appid);
                    appMod = appBll.SelReturnModel(appid);

                    uappMod = uappBll.SelModelByUid(mu.UserID, "wechat");
                    wuserMod = wxapi.GetWxUserModel(uappMod.OpenID);

                    userface.Src = wuserMod.HeadImgUrl;
                    userface.Alt = wuserMod.Name;
                    NickName.Text = wuserMod.Name;

                    string jsapi_ticket = APIHelper.GetWebResult("https://api.weixin.qq.com/cgi-bin/ticket/getticket?access_token=" + wxapi.AccessToken + "&type=jsapi");
                    JObject jsapi_obj = (JObject)JsonConvert.DeserializeObject(jsapi_ticket);
                    string stringA = "jsapi_ticket=" + jsapi_obj["ticket"].ToString() + "&noncestr=" + nonceStr + "&timestamp=" + timestr + "&url=" + Request.Url.AbsoluteUri;
                    paySign = EncryptHelper.SHA1(stringA).ToLower();

                    string jsapi_ticket1 = APIHelper.GetWebResult("https://api.weixin.qq.com/cgi-bin/qrcode/create?access_token=" + wxapi.AccessToken + "&type=jsapi", "POST", "{\"action_name\":\"QR_LIMIT_STR_SCENE\",\"action_info\":{\"scene\":{\"scene_str\":\"" + mu.UserID + "\"}}}");
                    if(jsapi_ticket1!="")
                    {
                        JObject jsapi_obj1 = (JObject)JsonConvert.DeserializeObject(jsapi_ticket1);
                        Image1.ImageUrl = "https://mp.weixin.qq.com/cgi-bin/showqrcode?ticket=" + jsapi_obj1["ticket"];
                    }
                }
                catch (Exception ex) 
				{
					
				}
            }
            else
            {
                Response.Redirect("/wxpromo.aspx?r=/User");
            }
        }
    }
	/// <summary>
    /// 检测推荐人
    /// </summary>
    /// <param name="puid">推荐人ID</param>
    /// <param name="uid">用户ID,未注册用户为0</param>
    /// <param name="err">错误信息</param>
    /// <returns>true:推荐人信息正常</returns>
    public bool CheckParentUser(int puid, int uid, ref string err)
    {
        //1,数据库中旧数据都是检测过的,不需要重检,所以只需要把好入口,对新用户的检测即可,即新用户的ParentUserID链中,不能有新用户ID
        //2,父ID是单线的不会有枝丫
        //3,子级仅需检测父ID是否包含在其子链当中即可
        M_UserInfo pmu = buser.SelReturnModel(puid);
        M_UserInfo mu = buser.SelReturnModel(uid);
        if (pmu.IsNull) { err = "推荐人不存在"; return false; }
        if (uid > 0 && puid == uid) { err = "推荐人ID不能同于用户ID"; return false; }
        if (!mu.IsNull)//在数据库中已有记录
        {
            //puid的父级链条中不能有该uid存在
            string puids = SelParentTree("ZL_User", "UserID", "ParentUserID", puid);
            string cuids = SelChildTree("ZL_User", "UserID", "ParentUserID", mu.UserID);
            //父级链条中不能包含当前用户ID
            if (!(puids.Split(',').FirstOrDefault(p => p.Equals(mu.UserID.ToString())) == null)) { err = "父级链中有用户[" + mu.UserName + "]存在"; return false; }
            if (!(cuids.Split(',').FirstOrDefault(p => p.Equals(mu.UserID.ToString())) == null)) { err = "子级链中有用户[" + pmu.UserName + "]存在"; return false; }
            return true;
        }
        return true;
    }
    /// <summary>
    /// 返回父级链,包含起始ID
    /// </summary>
    /// <param name="tbname">ZL_Node</param>
    /// <param name="pk">NodeID</param>
    /// <param name="pfield">示例:ParentID</param>
    /// <param name="startid">起始的ID值,如UserID的值</param>
    /// <param name="ids">返回的层级IDS</param>
    /// <returns></returns>
    private string SelParentTree(string tbname, string pk, string pfield, int startID)
    {
        string ids = "";
        if (startID < 1) { return ids; }
        string sql = "WITH f AS(SELECT * FROM {ZL_Node} WHERE {NodeID}=" + startID + " UNION ALL SELECT A.* FROM {ZL_Node} A, f WHERE a.{NodeID}=f.{ParentID}) SELECT * FROM {ZL_Node} WHERE {NodeID} IN(SELECT {NodeID} FROM f)";
        string oracle = "SELECT * FROM {tbname} START WITH {NodeID} =" + startID + " CONNECT BY PRIOR {ParentID} = {NodeID}";
        sql = sql.Replace("{ZL_Node}", tbname).Replace("{NodeID}", pk).Replace("{ParentID}", pfield);
        DataTable dt = DBCenter.ExecuteTable(DBCenter.GetSqlByDB(sql, oracle));
        foreach (DataRow dr in dt.Rows)
        {
            ids += dr[pk] + ",";
        }
        return ids.Trim(',');
    }
    /// <summary>
    /// 获取子级链,包含起始ID
    /// </summary>
    private string SelChildTree(string tbname, string pk, string pfield, int startID)
    {
        string ids = "";
        string sql = "WITH TREE as(SELECT * FROM {ZL_Node} WHERE {ParentID}=" + startID + " UNION ALL SELECT a.* FROM {ZL_Node} a JOIN Tree b on a.{ParentID}=b.{NodeID}) SELECT {NodeID} FROM Tree AS A";
        sql = sql.Replace("{ZL_Node}", tbname).Replace("{NodeID}", pk).Replace("{ParentID}", pfield);
        DataTable dt = DBCenter.ExecuteTable(sql);
        foreach (DataRow dr in dt.Rows)
        {
            ids += dr[pk] + ",";
        }
        return ids.Trim(',');
    }
}