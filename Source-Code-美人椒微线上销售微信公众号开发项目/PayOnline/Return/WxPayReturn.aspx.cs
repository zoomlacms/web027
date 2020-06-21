namespace ZoomLaCMS.PayOnline.Return
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using ZoomLa.BLL;
    using ZoomLa.BLL.Shop;
    using ZoomLa.BLL.User;
    using ZoomLa.BLL.WxPayAPI;
    using ZoomLa.Components;
    using ZoomLa.Model;
    using ZoomLa.SQLDAL;
    public partial class WxPayReturn : System.Web.UI.Page
    {
        private string PayPlat = "微信PC扫码|公众号支付";
        ZoomLa.BLL.WxPayAPI.Notify wxnotify = null;
        B_Payment payBll = new B_Payment();
        B_Order_PayLog paylogBll = new B_Order_PayLog();//支付日志类,用于记录用户付款信息
        B_OrderList orderBll = new B_OrderList();
        OrderCommon orderCom = new OrderCommon();
        B_User buser = new B_User();
        protected void Page_Load(object sender, EventArgs e)
        {
            wxnotify = new ZoomLa.BLL.WxPayAPI.Notify(this);
            string result = ProcessNotify();
            Response.Clear(); Response.Write(result); Response.Flush(); Response.End();
        }
        public string ProcessNotify()
        {
            //如果有多个公众号支付,此处要取返回中的公众号ID,再取Key验证
            WxPayData notifyData = wxnotify.GetNotifyData();
            WxPayData res = GetWxDataMod();
            //检查支付结果中transaction_id是否存在
            if (!notifyData.IsSet("out_trade_no"))
            {
                //若transaction_id不存在，则立即返回结果给微信支付后台
                res.SetValue("return_code", "FAIL");
                res.SetValue("return_msg", "支付结果中订单号不存在");
                ZLLog.L(ZLEnum.Log.pay, new M_Log()
                {
                    Action = "支付平台异常",
                    Message = PayPlat + ",原因:支付结果中订单号不存在!XML:" + notifyData.ToXml()
                });
                return res.ToXml();
            }
            string transaction_id = notifyData.GetValue("out_trade_no").ToString();
            //查询订单，判断订单真实性
            //if (!QueryOrder(transaction_id))
            //{
            //    //若订单查询失败，则立即返回结果给微信支付后台
            //    WxPayData res = GetWxDataMod();
            //    res.SetValue("return_code", "FAIL");
            //    res.SetValue("return_msg", "订单查询失败");
            //    ZLLog.L(ZLEnum.Log.pay, new M_Log()
            //    {
            //        Action = "支付平台异常",
            //        Message = PayPlat + ",支付单:" + transaction_id + ",原因:订单查询失败!XML:" + notifyData.ToXml()
            //    });
            //    return res.ToXml();
            //}
            //查询订单成功
            //else
            //{
            //}
            //未指定,则默认加载PC扫码配置
            M_Payment pinfo = payBll.SelModelByPayNo(notifyData.GetValue("out_trade_no").ToString());
            M_WX_APPID appMod = WxPayApi.Pay_GetByID(DataConvert.CLng(pinfo.PlatformInfo));
            notifyData.PayKey = appMod.Pay_Key;
            try
            {
                notifyData.CheckSign();
                PayOrder_Success(pinfo, notifyData);
            }
            catch (Exception ex)
            {
                ZLLog.L(ZLEnum.Log.pay, new M_Log() { Action = PayPlat + "报错,支付单:" + transaction_id, Message = ex.Message + "||XML:" + notifyData.ToXml() });
            }
            res.SetValue("return_code", "SUCCESS");
            res.SetValue("return_msg", "OK");
            return res.ToXml();
        }
        //支付成功时执行的操作
        private void PayOrder_Success(M_Payment pinfo,WxPayData result)
        {
            ZLLog.L(ZLEnum.Log.pay, PayPlat + " 支付单:" + result.GetValue("out_trade_no") + " 金额:" + result.GetValue("total_fee"));
            try
            {
                M_Order_PayLog paylogMod = new M_Order_PayLog();
                if (pinfo == null) { throw new Exception("支付单不存在"); }//支付单检测合为一个方法
                if (pinfo.Status != (int)M_Payment.PayStatus.NoPay) { throw new Exception("支付单状态不为未支付"); }
                pinfo.Status = (int)M_Payment.PayStatus.HasPayed;
                pinfo.PlatformInfo += PayPlat;
                pinfo.SuccessTime = DateTime.Now;
                pinfo.PayTime = DateTime.Now;
                pinfo.CStatus = true;
                //1=100,
                double tradeAmt = Convert.ToDouble(result.GetValue("total_fee")) / 100;
                pinfo.MoneyTrue = tradeAmt;
                payBll.Update(pinfo);
                DataTable orderDT = orderBll.GetOrderbyOrderNo(pinfo.PaymentNum);
                foreach (DataRow dr in orderDT.Rows)
                {
                    M_OrderList orderMod = orderBll.SelModelByOrderNo(dr["OrderNo"].ToString());
                    OrderHelper.FinalStep(pinfo, orderMod, paylogMod);
                    //if (orderMod.Ordertype == (int)M_OrderList.OrderEnum.Purse)
                    //{

                    //    M_UserInfo mu = buser.SelReturnModel(orderMod.Userid);
                    //    new B_Shop_MoneyRegular().AddMoneyByMin(mu, orderMod.Ordersamount, ",订单号[" + orderMod.OrderNo + "]");
                    //}
                    orderCom.SendMessage(orderMod, paylogMod, "payed");
                    //orderCom.SaveSnapShot(orderMod);
                    M_UserInfo mu = buser.GetUserByUserID(orderMod.Userid);
                    M_UserInfo pmu1 = buser.SelReturnModel(DataConvert.CLng(mu.ParentUserID));
                    M_UserInfo pmu2 = buser.SelReturnModel(DataConvert.CLng(pmu1.ParentUserID));
                    M_UserInfo pmu3 = buser.SelReturnModel(DataConvert.CLng(pmu2.ParentUserID));

                    //消息推送
                    B_User_Notify notifyBll = new B_User_Notify();
                    M_User_Notify notifyMod = new M_User_Notify();
                    notifyMod.CDate = DateTime.Now;

                    if (!pmu1.IsNull)
                    {
                        B_User_Notify.Add("订单购买消息", "您的初级乐享会员[会员ID：" + mu.UserID + ",微信名：" + mu.TrueName + "]下单成功，订单号为：" + orderMod.OrderNo, pmu1.UserID.ToString());
                    }
                    if (!pmu2.IsNull)
                    {
                        B_User_Notify.Add("订单购买消息", "您的中级乐享会员[会员ID：" + mu.UserID + ",微信名：" + mu.TrueName + "]下单成功，订单号为：" + orderMod.OrderNo, pmu1.UserID.ToString());
                    }
                    if (!pmu3.IsNull)
                    {
                        B_User_Notify.Add("订单购买消息", "您的高级乐享会员[会员ID：" + mu.UserID + ",微信名：" + mu.TrueName + "]下单成功，订单号为：" + orderMod.OrderNo, pmu1.UserID.ToString());
                    }
                    //订单返利
                    //M_UserInfo mu = buser.GetUserByUserID(orderMod.Userid);
                    //M_UserInfo pmu1 = buser.SelReturnModel(DataConvert.CLng(mu.ParentUserID));
                    //M_UserInfo pmu2 = buser.SelReturnModel(DataConvert.CLng(pmu1.ParentUserID));
                    //M_UserInfo pmu3 = buser.SelReturnModel(DataConvert.CLng(pmu2.ParentUserID));
                    //if (!pmu1.IsNull) { OrderRebates(pmu1.UserID, orderMod, mu); }
                    //if (!pmu2.IsNull) { OrderRebates(pmu2.UserID, orderMod, mu); }
                    //if (!pmu3.IsNull) { OrderRebates(pmu3.UserID, orderMod, mu); }
                }
                ZLLog.L(ZLEnum.Log.pay, PayPlat + "成功!支付单:" + result.GetValue("out_trade_no").ToString());
            }
            catch (Exception ex)
            {
                ZLLog.L(ZLEnum.Log.pay, new M_Log()
                {
                    Action = "支付回调报错",
                    Message = PayPlat + ",支付单:" + result.GetValue("out_trade_no").ToString() + ",原因:" + ex.Message
                });
            }
        }
        //查询订单
        private bool QueryOrder(string transaction_id)
        {
            WxPayData req = GetWxDataMod();
            req.SetValue("out_trade_no", transaction_id);
            WxPayData res = WxPayApi.OrderQuery(req, WxPayApi.Pay_GetByID());
            if (res.GetValue("return_code").ToString() == "SUCCESS" &&
                res.GetValue("result_code").ToString() == "SUCCESS")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private WxPayData GetWxDataMod() { return new WxPayData(); }
        //订单返利
        public int GetUserBuyNum(int userid)
        {
            DataTable dt = SqlHelper.ExecuteTable(CommandType.Text, "select * from ZL_Orderinfo where ID in (select Orderlistid from ZL_CartPro where ProID=4) And Paymentstatus=1 And Aside=0 And Userid=" + userid, null);
            return dt.Rows.Count;
        }
        public int GetFirstUser(int userid)
        {
            DataTable dt = SqlHelper.ExecuteTable(CommandType.Text, "select * from ZL_User where ParentUserID=" + userid + " And UserID in (select Userid from ZL_Orderinfo where ID in (select Orderlistid from ZL_CartPro where ProID=4) And Paymentstatus=1 And Aside=0)", null);
            return dt.Rows.Count;
        }
        public int GetSecondUser(int userid)
        {
            DataTable dt = SqlHelper.ExecuteTable(CommandType.Text, "select * from ZL_User where ParentUserID in (select UserID from ZL_User where ParentUserID=" + userid + ") And UserID in (select Userid from ZL_Orderinfo where ID in (select Orderlistid from ZL_CartPro where ProID=4) And Paymentstatus=1 And Aside=0)", null);
            return dt.Rows.Count;
        }
        public int GetThirdUser(int userid)
        {
            DataTable dt = SqlHelper.ExecuteTable(CommandType.Text, "select * from ZL_User where ParentUserID in (select UserID from ZL_User where ParentUserID in (select UserID from ZL_User where ParentUserID="+userid+")) And UserID in (select Userid from ZL_Orderinfo where ID in (select Orderlistid from ZL_CartPro where ProID=4) And Paymentstatus=1 And Aside=0)", null);
            return dt.Rows.Count;
        }
        public void OrderRebates(int userid, M_OrderList orderMod, M_UserInfo mu)
        {
            int buynum = GetUserBuyNum(userid);
            int fuser = GetFirstUser(userid);
            int suser = GetSecondUser(userid);
            int tuser = GetThirdUser(userid);

            //先判断自己是否购买众筹产品
            if (buynum > 0)
            {
                //一级有效会员大于10人，二级有效会员小于21人 获得银币（只能用于购买众筹商品）
                if (fuser >= 10 && suser < 21)
                {
                    //第一层是产品
                    buser.AddMoney(userid, 99, M_UserExpHis.SType.SIcon, "返利[第一层], 来源: " + mu.UserName + "[用户ID：" + mu.UserID + "][订单号：" + orderMod.OrderNo + "], 备注：99");
                }
                //一级有效会员大于10人，二级有效会员大于21人 三级有效会员小于32人 二级会员每人提成9.9元
                if (fuser >= 10 && suser >= 21 && tuser < 32)
                {
                    //第一级别：9.9元
                    buser.AddMoney(userid, 9.9 * suser, M_UserExpHis.SType.Purse, "返利[第二层第一级别]，来源: " + mu.UserName + "[用户ID：" + mu.UserID + "][订单号：" + orderMod.OrderNo + "], 备注：" + (9.9 * suser).ToString("f2"));
                }
                //一级有效会员大于10人，二级有效会员大于21人 三级有效会员大于32人 三级会员每人提成5元
                if (fuser >= 10 && suser >= 21 && tuser >= 32)
                {
                    //第二级别：5元
                    buser.AddMoney(userid, 5 * tuser, M_UserExpHis.SType.Purse, "返利[第二层第二级别]，来源: " + mu.UserName + "[用户ID：" + mu.UserID + "][订单号：" + orderMod.OrderNo + "], 备注：" + (5 * suser).ToString("f2"));
                }
            }
        }
    }
}