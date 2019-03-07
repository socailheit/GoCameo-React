using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Dynamic;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Cors;
using System.Net.Http;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authentication;
using GoCameo.Server.Business;

namespace GoCameo.Server.Api
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class service : Controller
    {
        private IHostingEnvironment _env;

        public service(IHostingEnvironment env)
        {
            _env = env;
        }

        [DisableCors]
        [HttpGet("get")]
        public async Task<object> Get(string param)
        {
            Int64? length = Request.ContentLength;
            //We will make a GET request to a really cool website...
            string responceparam = "";
            string authtoken = ((Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.FrameRequestHeaders)Request.Headers).HeaderAuthorization.FirstOrDefault();
            string baseUrl = ServiceBL.GetBaseUrl();
            dynamic objParam = Newtonsoft.Json.Linq.JObject.Parse(param);
            if (Convert.ToString(objParam.url).StartsWith("http://baseurl/"))
            {

                objParam.url = Convert.ToString(objParam.url).Replace("http://baseurl/", baseUrl.ToLower());
            }
            else if (Convert.ToString(objParam.url).StartsWith("http://clienturl/"))
            {

                objParam.url = Convert.ToString(objParam.url).Replace("http://clienturl/", AzureAdB2COptions.Settings.authapiturl);

            }
            if (!string.IsNullOrEmpty(Convert.ToString(objParam.url)))
            {
                UriBuilder builder = new UriBuilder(Convert.ToString(objParam.url));

                foreach (var item in objParam)
                {
                    if (Convert.ToString(item.Name) != "url" && Convert.ToString(item.Name) != "undefined")
                    {
                        if (Convert.ToString(item.Name) == "response")
                        {
                            responceparam = Convert.ToString(item.Value);
                        }
                        else
                        {
                            if (builder.Query == "")
                                builder.Query = Convert.ToString(item.Name) + "=" + Convert.ToString(item.Value);
                            else
                                builder.Query = builder.Query + "&" + Convert.ToString(item.Name) + "=" + Convert.ToString(item.Value);
                        }
                    }
                }
                //  var Token = HttpContext.Session.GetString(Utility.SToken);

                var requestUserTimeline = new HttpRequestMessage(HttpMethod.Get, builder.Uri);
                if (!string.IsNullOrEmpty(authtoken))
                {
                    requestUserTimeline.Headers.Add("Authorization", authtoken);
                }
                //try
                //{
                //  requestUserTimeline = await AddTokenAsync(requestUserTimeline);

                //}
                //catch
                //{


                //}

                var httpClient = new HttpClient();
                HttpResponseMessage responseUserTimeLine = await httpClient.SendAsync(requestUserTimeline);

                dynamic json = JsonConvert.DeserializeObject(await responseUserTimeLine.Content.ReadAsStringAsync());

                try
                {
                    if (json is string)
                    {
                        dynamic obj = new ExpandoObject();
                        obj.data = json;
                        return Json(obj);
                    }
                    else if ((json.responsecode == 401 || Convert.ToString(json.message).ToLower() == "access denied"))
                    {
                        return Unauthorized();
                    }
                    else if (json.responsecode == 200)
                    {
                        dynamic datalist = json.response;
                        if (string.IsNullOrEmpty(responceparam))
                        {
                            return json.response;
                        }
                        else
                        {
                            if (responceparam == "prioritylist")
                            {
                                return datalist[0].prioritylist;
                            }
                            else
                            {
                                var ret = json.response[responceparam];
                                return ret;
                            }

                        }
                        // datalist.categories;
                    }
                    else
                    {
                        return Json(new { });
                    }
                }
                catch (Exception ex)
                {

                    throw;
                }



            }

            return Json(new { });
        }

        [DisableCors]
        [HttpPost("post")]
        public async Task<IActionResult> Post([FromBody]string data, string param)
        {
            dynamic objData = Newtonsoft.Json.Linq.JObject.Parse(data);
            dynamic message = new ExpandoObject();
            try
            {
                dynamic objParam = Newtonsoft.Json.Linq.JObject.Parse(param);
                string baseUrl = ServiceBL.GetBaseUrl();
                if (Convert.ToString(objParam.url).StartsWith("http://baseurl/"))
                {

                    objParam.url = Convert.ToString(objParam.url).Replace("http://baseurl/", baseUrl.ToLower());
                }
                UriBuilder builder = new UriBuilder(Convert.ToString(objParam.url));
                var isApiResponseRequired = false;
                var responceparam = "";
                foreach (var item in objParam)
                {
                    if (Convert.ToString(item.Name) != "response" && Convert.ToString(item.Name) != "url" && Convert.ToString(item.Name) != "undefined")
                    {
                        if (builder.Query == "")
                            builder.Query = Convert.ToString(item.Name) + "=" + Convert.ToString(item.Value);
                        else
                            builder.Query = builder.Query + "&" + Convert.ToString(item.Name) + "=" + Convert.ToString(item.Value);
                    }
                    else if (Convert.ToString(item.Name) == "response")
                    {
                        isApiResponseRequired = true;
                        if (item.Value != "")
                            responceparam = Convert.ToString(item.Value);
                    }
                }

                var content = ServiceBL.GetContentstring(objData);
                //  var Token = HttpContext.Session.GetString(Utility.SToken);
                var Method = HttpMethod.Post;
                if (param.Contains("UpdateRequisitionAsync"))
                {
                    Method = HttpMethod.Put;
                }
                else if (param.Contains("DeleteRequisitionAttachmentAsync") || param.Contains("DeleteRequisitionAsync") || param.Contains("DeleteVMOAsync")
                   || param.Contains("DeleteVMOAccessAsync") || param.Contains("DeleteCandidateResumeAsync"))
                {
                    Method = HttpMethod.Delete;
                }
                var requestUserTimeline = new HttpRequestMessage(Method, builder.Uri);
                string authtoken = ((Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.FrameRequestHeaders)Request.Headers).HeaderAuthorization.FirstOrDefault();
                requestUserTimeline.Headers.Add("Authorization", authtoken);
                //try
                //{
                //  requestUserTimeline = await AddTokenAsync(requestUserTimeline);

                //}
                //catch
                //{


                //}
                if (Method != HttpMethod.Delete)
                {
                    requestUserTimeline.Content = content;
                }
                var httpClient = new HttpClient();
                HttpResponseMessage responseUserTimeLine = await httpClient.SendAsync(requestUserTimeline);

                dynamic json = JsonConvert.DeserializeObject(await responseUserTimeLine.Content.ReadAsStringAsync());
                List<string> _highlightKeyword = new List<string>();
                if (objParam.url.ToString().Contains("GetCandidateByKeywordSearchAsync") || objParam.url.ToString().Contains("GetCBCandidateListBySearchKeywordsAsync") || objParam.url.ToString().Contains("GetDiceCandidateListBySearchKeywordsAsync") || objParam.url.ToString().Contains("GetMonsterSearchCandidateListAsync"))
                {
                    var searchString = "";
                    foreach (var item in objData)
                    {
                        if (!string.IsNullOrEmpty(Convert.ToString(item.Value)))
                        {
                            var type = item.Value.GetType();
                            int Num;
                            bool isNum = int.TryParse(item.Value.ToString(), out Num);
                            if (type.ToString() != "Newtonsoft.Json.Linq.JArray" && !isNum && item.Name.ToString().ToLower() != "country" && item.Name.ToString().ToLower() != "searchpattern" && item.Value.ToString().ToLower() != "no" && item.Value.ToString().ToLower() != "yes")
                            {
                                searchString = searchString + ", " + item.Value;
                            }
                        }
                    }
                    if (searchString != "")
                    {
                        searchString = searchString.Replace("/", ", ");
                    }
                    _highlightKeyword = ParseSearchQuery(searchString);
                }
                if (json != null && (json.responsecode == 401 || Convert.ToString(json.message).ToLower() == "access denied"))
                {
                    return Unauthorized();
                }
                else
               if (json != null && json.responsecode == 200)
                {
                    if (isApiResponseRequired == true)
                    {
                        dynamic datalist = json.response;
                        if (string.IsNullOrEmpty(responceparam))
                        {
                            if (_highlightKeyword.Count > 0 && datalist.GetType().ToString() == "Newtonsoft.Json.Linq.JObject")
                            {
                                string output = JsonConvert.SerializeObject(_highlightKeyword);
                                datalist.Add("highlightkeyword", new JValue(output));
                            }
                            return Json(datalist);
                        }
                        else
                        {
                            if (responceparam == "prioritylist")
                            {
                                return Json(datalist[0].prioritylist);
                            }
                            else
                            {
                                var ret = json.response[responceparam];
                                return Json(ret);
                            }
                        }
                    }
                    else
                    {
                        message.status = "success";
                        message.message = "Data saved successfully.";
                        message.data = json.response;
                        return Json(message);
                    }
                }
                else
                {
                    message.status = "error";
                    if (json != null)
                    {
                        message.message = Convert.ToString(json.message);
                    }
                    else
                    {
                        message.message = "Data save unsuccessfull. Please try again.";
                    }

                    return Json(message);
                }
            }
            catch (Exception ex)
            {
                message.status = "error";
                message.message = "An unexpected error occurred. Please try again.If the problem continues, contact support.";
                return Json(message);
            }

        }
        [DisableCors]
        [HttpPost("publishasync")]
        public async Task<IActionResult> JobPortalPublishAsync([FromBody]string data, string param)
        {
            dynamic objData = Newtonsoft.Json.Linq.JObject.Parse(data);
            dynamic objData_local = Newtonsoft.Json.Linq.JObject.Parse("{}");
            dynamic message = new ExpandoObject();
            // dynamic objParam = Newtonsoft.Json.Linq.JObject.Parse(param);
            string baseUrl = ServiceBL.GetBaseUrl();
            var apiName = "";
            var urlList = new List<string>();
            urlList.Add("http://baseurl/api/JobPortalPublish/CreateCBJobPortalPublishAsync");
            urlList.Add("http://baseurl/api/JobPortalPublish/CreateJobPortalPublishAsync");
            foreach (string url in urlList)
            {

                if (url.Contains("CreateJobPortalPublishAsync"))
                {
                    apiName = "Collabera Job";
                }
                else
                {
                    apiName = "Career Builder Job";
                }
                try
                {
                    var _url = Convert.ToString(url).Replace("http://baseurl/", baseUrl.ToLower());
                    //if (Convert.ToString(url).StartsWith("http://baseurl/"))
                    //{
                    //  objParam.url = Convert.ToString(url).Replace("http://baseurl/", baseUrl.ToLower());
                    //}
                    UriBuilder builder = new UriBuilder(Convert.ToString(_url));
                    //var isApiResponseRequired = false;
                    //var responceparam = "";
                    //foreach (var item in objParam)
                    //{
                    //  if (Convert.ToString(item.Name) != "response" && Convert.ToString(item.Name) != "url" && Convert.ToString(item.Name) != "undefined")
                    //  {
                    //    if (builder.Query == "")
                    //      builder.Query = Convert.ToString(item.Name) + "=" + Convert.ToString(item.Value);
                    //    else
                    //      builder.Query = builder.Query + "&" + Convert.ToString(item.Name) + "=" + Convert.ToString(item.Value);
                    //  }
                    //}
                    if (url.Contains("CreateJobPortalPublishAsync"))
                    {
                        objData_local.clientid = objData.clientid;
                        objData_local.jobcode = objData.jobcode;
                        objData_local.categoryid = objData.categorytypeid;
                        objData_local.category = objData.categorytype;
                        objData_local.experiencetypeid = objData.experiencetypeid;
                        objData_local.industrytype = objData.industrytype;
                        objData_local.industrytypeid = objData.industrytypeid;
                        objData_local.requisitionid = objData.requisitionid;
                        objData_local.requisitionname = objData.requisitionname;
                        objData_local.jobtypeid = objData.jobtypeid;
                        objData_local.jobtype = objData.jobtype;
                        objData_local.positiontypeid = null;// objData.positiontypeid;
                        objData_local.positiontype = objData.positiontypeid_node != null ? objData.positiontypeid_node.categorized : "";
                        objData_local.domain = objData.domain;
                        objData_local.description = objData.description;
                        objData_local.recruitername = objData.recruitername;
                        objData_local.recruitercity = objData.recruitercity;
                        objData_local.recruiterstate = objData.recruiterstate;
                        objData_local.recruitercountry = objData.recruitercountry;
                        objData_local.recruiteremail = objData.recruiteremail;
                        objData_local.recruitercontactnumber = objData.recruitercontactnumber;
                        objData_local.recruiteremailcc = objData.recruiteremailcc;
                        objData_local.payrateUnit = objData.payrateUnit;
                        objData_local.degreetypeid = objData.degreetypeid;
                        objData_local.degreetype = objData.degreetype;
                        objData_local.traveltypeid = objData.traveltypeid;
                        objData_local.traveltype = objData.traveltype;
                        // objData_local.jobportal = objData.jobportal.ToString() != "" ? objData.jobportal : JObject.Parse("[]");
                        objData_local.jobportal = JArray.Parse("[1]");

                        objData_local.payratefrom = objData.payratefrom;
                        objData_local.payrateto = objData.payrateto;
                        objData_local.linkedinid = objData.linkedinid;
                        // objData_local.primaryskills = objData.primaryskills.ToString() != "" ? objData.primaryskills : JObject.Parse("[]");
                        objData_local.primaryskills = objData.primaryskills;
                        objData_local.secondaryskills = objData.secondaryskills;

                        objData_local.jobtitleid = objData.jobtitleid;
                        objData_local.jobtitle = objData.jobtitle;
                        objData_local.tcuid = objData.tcuid;
                        objData_local.city = objData.city;
                        objData_local.state = objData.state;
                        objData_local.zipcode = objData.zipcode;
                        objData_local.countrycode = objData.countrycode;
                        objData_local.minimumexperience = string.IsNullOrEmpty(Convert.ToString(objData.minimumexperience)) ? 0 : objData.minimumexperience;
                        objData_local.maximumexperience = string.IsNullOrEmpty(Convert.ToString(objData.maximumexperience)) ? 0 : objData.maximumexperience;
                        objData_local.ispermanent = objData.ispermanent;

                    }
                    else
                    {
                        objData_local = objData;
                        objData_local.jobportal = JArray.Parse("[2]");
                    }
                    // objData.Remove("categorytype");
                    var content = ServiceBL.GetContentstring(objData_local);
                    //  var Token = HttpContext.Session.GetString(Utility.SToken);
                    var Method = HttpMethod.Post;

                    var requestUserTimeline = new HttpRequestMessage(Method, builder.Uri);
                    string authtoken = ((Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.FrameRequestHeaders)Request.Headers).HeaderAuthorization.FirstOrDefault();
                    requestUserTimeline.Headers.Add("Authorization", authtoken);
                    requestUserTimeline.Headers.Add("ContentType", "application/json");

                    requestUserTimeline.Content = content;

                    var httpClient = new HttpClient();
                    HttpResponseMessage responseUserTimeLine = await httpClient.SendAsync(requestUserTimeline);

                    dynamic json = JsonConvert.DeserializeObject(await responseUserTimeLine.Content.ReadAsStringAsync());

                    if (json != null && (json.responsecode == 401 || Convert.ToString(json.message).ToLower() == "access denied"))
                    {
                        return Unauthorized();
                    }
                    else if (json != null && json.responsecode == 200)
                    {
                        message.status = "success";
                        message.message = "Data saved successfully.";
                        message.data = json.response;
                        // return Json(message);
                    }
                    else
                    {
                        message.status = "error";
                        if (json != null)
                        {
                            if (apiName == "Career Builder Job")
                            {
                                message.message = "1. Error while submitting " + apiName + ", " + Convert.ToString(json.message);
                            }
                            else
                            {
                                message.message = message.message + "\r\n 2. Error while submitting " + apiName + ", " + Convert.ToString(json.message);
                            }
                        }
                        else
                        {
                            if (apiName == "Career Builder Job")
                            {
                                message.message = "1. Error while submitting " + apiName + ", Data save unsuccessfull. Please try again.";
                            }
                            else
                            {
                                message.message = message.message + "\r\n 2. Error while submitting " + apiName + ", Data save unsuccessfull. Please try again.";
                            }
                        }

                        // return Json(message);
                    }
                }
                catch (Exception ex)
                {
                    message.status = "error";
                    message.message = "An unexpected error occurred. Please try again.If the problem continues, contact support. Error while submitting " + apiName;
                    return Json(message);
                }
            }
            return Json(message);
        }
        [DisableCors]
        [HttpGet("getrequisition")]
        public async Task<object> GetRequisition(string param)
        {
            param = !string.IsNullOrEmpty(Request.QueryString.Value) ? Request.QueryString.Value.Substring(1) : "";
            var data = param.Split("&");
            dynamic Params = Newtonsoft.Json.Linq.JObject.Parse("{}");
            foreach (var item in data)
            {
                var _item = item.Split("=");
                Params[_item[0]] = _item[1];
            }
            Int64? length = Request.ContentLength;
            //We will make a GET request to a really cool website...
            var Method = HttpMethod.Get;
            string responceparam = "";
            string authtoken = ((Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.FrameRequestHeaders)Request.Headers).HeaderAuthorization.FirstOrDefault();
            string baseUrl = ServiceBL.GetBaseUrl();
            dynamic objParam = Newtonsoft.Json.Linq.JObject.Parse("{'url':''}");
            string contentStr = "";
            if (Params.url == "GetRequisitionByUserAsync")
            {
                Method = HttpMethod.Get;
                objParam.url = Convert.ToString("http://baseurl/api/Requisition/GetRequisitionByUserAsync").Replace("http://baseurl/", baseUrl.ToLower());
                objParam.assignedto = Params.assignedto;
                objParam.pageindex = Params.pageindex;
                objParam.pagesize = Params.pagesize;
                objParam.orderby = Params.orderby;
                objParam.sortexpression = Params.sortexpression;
                objParam.response = Params.response;
            }
            else if (Params.url == "RequisitionSearchAsync")
            {
                Method = HttpMethod.Post;
                objParam.url = Convert.ToString("http://baseurl/api/Requisition/RequisitionSearchAsync").Replace("http://baseurl/", baseUrl.ToLower());
                objParam.response = Params.response;
                //content.SearchText = SearchText;
                //content.PageIndex = PageIndex;
                //content.PageSize = PageSize;
                //content.ReceivedFrom = ReceivedFrom;
                //content.ReceivedTo = ReceivedTo;
                String searchtext = Uri.UnescapeDataString(Params.searchtext.ToString());
                contentStr = "{\"type\":\"" + Params.type + "\",\"searchtext\":\"" + searchtext + "\",\"pageindex\":" + Params.pageindex + ",\"pagesize\":" + Params.pagesize + ",\"receivedfrom\":\"" + Params.receivedfrom + "\",\"receivedto\":\"" + Params.receivedto + "\"}";
                //contentstr = "{\"candidateid\":" + objData.candidateid + ",\"requisitionid\":\"" + objData.requisitionid + "\",\"candidatestatus\":\"In Process\"}";
            }
            else
            {
                return Json(new { });
            }
            if (!string.IsNullOrEmpty(Convert.ToString(objParam.url)))
            {
                UriBuilder builder = new UriBuilder(Convert.ToString(objParam.url));

                foreach (var item in objParam)
                {
                    if (Convert.ToString(item.Name) != "url" && Convert.ToString(item.Name) != "undefined")
                    {
                        if (Convert.ToString(item.Name) == "response")
                        {
                            responceparam = Convert.ToString(item.Value);
                        }
                        else
                        {
                            if (builder.Query == "")
                                builder.Query = Convert.ToString(item.Name) + "=" + Convert.ToString(item.Value);
                            else
                                builder.Query = builder.Query + "&" + Convert.ToString(item.Name) + "=" + Convert.ToString(item.Value);
                        }
                    }
                }
                //  var Token = HttpContext.Session.GetString(Utility.SToken);

                var requestUserTimeline = new HttpRequestMessage(Method, builder.Uri);
                if (!string.IsNullOrEmpty(authtoken))
                {
                    requestUserTimeline.Headers.Add("Authorization", authtoken);
                }
                //try
                //{
                //  requestUserTimeline = await AddTokenAsync(requestUserTimeline);

                //}
                //catch
                //{


                //}
                try
                {
                    if (Method == HttpMethod.Post)
                    {
                        dynamic obj = Newtonsoft.Json.Linq.JObject.Parse(contentStr);
                        var content = ServiceBL.GetContentstring(obj);
                        requestUserTimeline.Content = content;
                    }
                    var httpClient = new HttpClient();
                    HttpResponseMessage responseUserTimeLine = await httpClient.SendAsync(requestUserTimeline);

                    dynamic json = JsonConvert.DeserializeObject(await responseUserTimeLine.Content.ReadAsStringAsync());

                    if (json != null && (json.responsecode == 401 || Convert.ToString(json.message).ToLower() == "access denied"))
                    {
                        return Unauthorized();
                    }
                    else if (json is string)
                    {
                        dynamic obj = new ExpandoObject();
                        obj.data = json;
                        return Json(obj);
                    }
                    else if (json.responsecode == 200)
                    {
                        dynamic datalist = json.response;
                        if (string.IsNullOrEmpty(responceparam))
                        {
                            return json.response;
                        }
                        else
                        {
                            if (responceparam == "prioritylist")
                            {
                                return datalist[0].prioritylist;
                            }
                            else
                            {
                                var ret = json.response[responceparam];
                                return ret;
                            }

                        }
                        // datalist.categories;
                    }
                    else
                    {
                        return Json(new { });
                    }
                }
                catch (Exception ex)
                {

                    throw;
                }



            }
            return Json(new { });
        }
        [DisableCors]
        [HttpPost("submitcandidate")]
        public async Task<IActionResult> submitcandidate([FromBody]string data, string param)
        {
            dynamic objData = Newtonsoft.Json.Linq.JObject.Parse(data);
            dynamic message = new ExpandoObject();
            try
            {
                // dynamic objParam = Newtonsoft.Json.Linq.JObject.Parse(param);
                string baseUrl = ServiceBL.GetBaseUrl();
                baseUrl = baseUrl + "api/Candidate/SubmitCandidateCOAndSubmissionCheckListAsync";
                //if (Convert.ToString(objParam.url).StartsWith("http://baseurl/"))
                //{

                //  objParam.url = Convert.ToString(objParam.url).Replace("http://baseurl/", baseUrl.ToLower());
                //}
                UriBuilder builder = new UriBuilder(Convert.ToString(baseUrl));
                //foreach (var item in objParam)
                //{
                //  if (Convert.ToString(item.Name) != "url" && Convert.ToString(item.Name) != "undefined")
                //  {
                //    if (builder.Query == "")
                //      builder.Query = Convert.ToString(item.Name) + "=" + Convert.ToString(item.Value);
                //    else
                //      builder.Query = builder.Query + "&" + Convert.ToString(item.Name) + "=" + Convert.ToString(item.Value);
                //  }
                //}
                string contentstr = "{\"candidateid\":" + objData.candidateid + ",\"requisitionid\":\"" + objData.requisitionid + "\",\"candidatestatus\":\"In Process\"}";
                dynamic obj = Newtonsoft.Json.Linq.JObject.Parse(contentstr);

                var content = ServiceBL.GetContentstring(obj);
                // var content = GetContentstring(objData);
                //  var Token = HttpContext.Session.GetString(Utility.SToken);
                var Method = HttpMethod.Post;
                var requestUserTimeline = new HttpRequestMessage(Method, builder.Uri);
                string authtoken = ((Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.FrameRequestHeaders)Request.Headers).HeaderAuthorization.FirstOrDefault();
                requestUserTimeline.Headers.Add("Authorization", authtoken);

                //try
                //{
                //  requestUserTimeline = await AddTokenAsync(requestUserTimeline);

                //}
                //catch
                //{


                //}
                requestUserTimeline.Content = content;
                var httpClient = new HttpClient();
                HttpResponseMessage responseUserTimeLine = await httpClient.SendAsync(requestUserTimeline);

                dynamic json = JsonConvert.DeserializeObject(await responseUserTimeLine.Content.ReadAsStringAsync());
                if (json != null && (json.responsecode == 401 || Convert.ToString(json.message).ToLower() == "access denied"))
                {
                    return Unauthorized();
                }
                else
               if (json != null && json.responsecode == 200)
                {
                    message.status = "success";
                    message.message = "Data saved successfully.";
                    return Json(message);
                }
                else
                {
                    message.status = "error";
                    if (json != null)
                    {
                        message.message = Convert.ToString(json.message);
                    }
                    else
                    {
                        message.message = "Data save unsuccessfull. Please try again.";
                    }

                    return Json(message);
                }
            }
            catch (Exception ex)
            {
                message.status = "error";
                message.message = "An unexpected error occurred. Please try again.If the problem continues, contact support.";
                return Json(message);
            }

        }
        [DisableCors]
        [HttpPost("uploadfiles")]
        public async Task<object> uploadfiles(IFormFile file, string requisitionid)
        {
            try
            {

                //if (file.FileName.Contains(".pdf") || file.FileName.Contains(".xls") || file.FileName.Contains(".docx") || file.FileName.Contains(".txt"))
                //{
                dynamic message = new ExpandoObject();
                string baseUrl = ServiceBL.GetBaseUrl();
                baseUrl = baseUrl + "api/Requisition/UploadRequisitionAttachmentsAsync";
                using (var httpClient = new HttpClient())
                {
                    string authtoken = ((Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.FrameRequestHeaders)Request.Headers).HeaderAuthorization.FirstOrDefault();
                    //requestUserTimeline.Headers.Add("Authorization", "bearer " + authtoken);
                    //TokenViewModel Token = new TokenViewModel();
                    //Token = HttpContext.Session.GetObject<TokenViewModel>(Utility.Saccesstoken);
                    httpClient.DefaultRequestHeaders.Add("Authorization", authtoken);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));


                    using (var content = new MultipartFormDataContent())
                    {
                        foreach (var item in Request.Form.Files)
                        {
                            content.Add(CreateFileContent(item.OpenReadStream(), item.FileName, item.ContentType));

                        }
                        content.Add(new StringContent(requisitionid), "requisitionid");

                        var response = await httpClient.PostAsync(baseUrl, content);
                        response.EnsureSuccessStatusCode();


                        dynamic json = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
                        if (json != null && (json.responsecode == 401 || Convert.ToString(json.message).ToLower() == "access denied"))
                        {
                            return Unauthorized();
                        }
                        else
                       if (json.responsecode == 200)
                        {
                            message.status = "success";
                            message.message = "Data saved successfully.";

                            return Json(message);
                        }
                        else
                        {
                            message.status = "error";
                            message.message = json.message;

                            return Json(message);

                        }
                    }
                }



            }
            catch (Exception ex)
            {
                dynamic message = new ExpandoObject();
                message.status = "error";
                message.message = "Please upload file again.";

                return Json(message);
            }
        }
        [DisableCors]
        [HttpPost("uploadcandidatefiles")]
        public async Task<object> uploadcandidatefiles(IFormFile file, string candidateId)
        {
            try
            {
                dynamic message = new ExpandoObject();
                file = Request.Form.Files.Count > 0 ? Request.Form.Files[0] : null;
                if (file != null && file.Length > 0)
                {
                    if (file.FileName.Contains(".pdf") || file.FileName.Contains(".docx"))
                    {
                        string baseUrl = ServiceBL.GetBaseUrl();
                        //baseUrl = baseUrl + "api/Candidate/UploadCandidateResumeAsync";
                        string url = baseUrl + "api/Candidate/UploadCandidateResumeAsync?candidateId=" + candidateId;
                        using (var httpClient = new HttpClient())
                        {
                            byte[] surveyBytes;
                            using (var br = new BinaryReader(file.OpenReadStream()))
                                surveyBytes = br.ReadBytes((int)file.OpenReadStream().Length);

                            // ByteArrayContent bytes = new ByteArrayContent(surveyBytes);
                            // var Token = HttpContext.Session.GetString(Utility.SToken);
                            string authtoken = ((Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.FrameRequestHeaders)Request.Headers).HeaderAuthorization.FirstOrDefault();
                            httpClient.DefaultRequestHeaders.Add("Authorization", authtoken);
                            // httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authtoken);
                            // httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                            var byteArrayContent = new ByteArrayContent(surveyBytes);
                            byteArrayContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");

                            var response = httpClient.PostAsync(url, new MultipartFormDataContent { { byteArrayContent, "\"file\"", file.FileName } }).Result;
                            dynamic json = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
                            if (json != null && (json.code == 200 || json.responsecode == 200))
                            {
                                message.status = "success";
                                message.message = "Data saved successfully.";

                                return Json(message);
                            }
                            else
                            {
                                message.status = "error";
                                message.message = json == null ? response.StatusCode + " " + response.ReasonPhrase : json.message;

                                return Json(message);
                            }
                        }
                    }
                    else
                    {
                        message.status = "error";
                        message.message = "Invalid file";

                        return Json(message);
                    }

                }
                else
                {
                    return "";
                }

            }
            catch (Exception ex)
            {
                dynamic message = new ExpandoObject();
                message.status = "error";
                message.message = "Please upload file again.";

                return Json(message);
            }
        }

        private StreamContent CreateFileContent(Stream stream, string fileName, string contentType)
        {
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "\"files\"",
                FileName = "\"" + fileName + "\""
            }; // the extra quotes are key here
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            return fileContent;
        }
        [DisableCors]
        [HttpGet("GoCameostaticremider")]
        public object GoCameostaticRemider(string Time)
        {
            try
            {
                string FormStr = "";
                // var _time = String.IsNullOrEmpty(Time) ? null : Time.Substring(0, 5);
                //var timeUtc = DateTime.Parse(Time);
                //TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                //DateTime easternTime = TimeZoneInfo.ConvertTime(timeUtc, easternZone);
                var _time = String.IsNullOrEmpty(Time) ? null : Time.Substring(Time.IndexOf(" ") + 1, 5).Replace(":", ".");
                StreamReader oRead;
                string filepath = System.IO.Path.Combine(_env.ContentRootPath, "wwwroot/assets/js", "staticreminder.json");

                if (System.IO.File.Exists(filepath))
                {
                    filepath = System.IO.Path.Combine(_env.ContentRootPath, "wwwroot/assets/js");
                    oRead = System.IO.File.OpenText(Path.Combine(filepath, "staticreminder.json"));  //For Local
                    FormStr = oRead.ReadToEnd();
                    oRead.Close();
                }
                dynamic FormObj = JsonConvert.DeserializeObject(FormStr);
                if (!String.IsNullOrEmpty(Time))
                {
                    foreach (var item in FormObj)
                    {
                        if (Convert.ToDouble(Convert.ToString(item.fromTime)) <= Convert.ToDouble(_time) && Convert.ToDouble(_time) < Convert.ToDouble(Convert.ToString(item.toTime)))
                        {
                            List<object> objList = new List<object>();
                            objList.Add(item);
                            return objList;
                        }
                    }
                }
                return Json(FormObj);
            }
            catch (Exception ex)
            {

                ModelState.AddModelError(string.Empty, "Please try after sometime");
                return BadRequest(ModelState);
            }

        }
        [DisableCors]
        [HttpGet("GoCameostaticremiderupcoming")]
        public object GoCameostaticRemiderUpcoming(string Time)
        {
            try
            {
                string FormStr = "";
                // var _time = String.IsNullOrEmpty(Time) ? null : Time.Substring(0, 5);
                //var timeUtc = DateTime.Parse(Time);
                //TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                //DateTime easternTime = TimeZoneInfo.ConvertTime(timeUtc, easternZone);
                var _time = String.IsNullOrEmpty(Time) ? null : Time.Substring(Time.IndexOf(" ") + 1, 5).Replace(":", ".");
                StreamReader oRead;
                string filepath = System.IO.Path.Combine(_env.ContentRootPath, "wwwroot/assets/js", "staticreminder.json");

                if (System.IO.File.Exists(filepath))
                {
                    filepath = System.IO.Path.Combine(_env.ContentRootPath, "wwwroot/assets/js");
                    oRead = System.IO.File.OpenText(Path.Combine(filepath, "staticreminder.json"));  //For Local
                    FormStr = oRead.ReadToEnd();
                    oRead.Close();
                }
                List<object> objList = new List<object>();
                dynamic FormObj = JsonConvert.DeserializeObject(FormStr);
                if (!String.IsNullOrEmpty(Time))
                {
                    foreach (var item in FormObj)
                    {
                        if (objList.Count == 2)
                        {
                            return objList;
                        }
                        else if (Convert.ToDouble(Convert.ToString(item.fromTime)) > Convert.ToDouble(_time))
                        {
                            objList.Add(item);
                        }
                    }
                }
                return objList;
            }
            catch (Exception ex)
            {

                ModelState.AddModelError(string.Empty, "Please try after sometime");
                return BadRequest(ModelState);
            }

        }
        [DisableCors]
        [HttpGet("GoCameodmstaticremider")]
        public object GoCameoDMstaticRemider(string Time)
        {
            try
            {
                string FormStr = "";
                // var _time = String.IsNullOrEmpty(Time) ? null : Time.Substring(0, 5);
                //var timeUtc = DateTime.Parse(Time);
                //TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                //DateTime easternTime = TimeZoneInfo.ConvertTime(timeUtc, easternZone);
                var _time = String.IsNullOrEmpty(Time) ? null : Time.Substring(Time.IndexOf(" ") + 1, 5).Replace(":", ".");
                StreamReader oRead;
                string filepath = System.IO.Path.Combine(_env.ContentRootPath, "wwwroot/assets/js", "staticreminderdm.json");

                if (System.IO.File.Exists(filepath))
                {
                    filepath = System.IO.Path.Combine(_env.ContentRootPath, "wwwroot/assets/js");
                    oRead = System.IO.File.OpenText(Path.Combine(filepath, "staticreminderdm.json"));  //For Local
                    FormStr = oRead.ReadToEnd();
                    oRead.Close();
                }
                dynamic FormObj = JsonConvert.DeserializeObject(FormStr);
                if (!String.IsNullOrEmpty(Time))
                {
                    foreach (var item in FormObj)
                    {
                        if (Convert.ToDouble(Convert.ToString(item.fromTime)) <= Convert.ToDouble(_time) && Convert.ToDouble(_time) < Convert.ToDouble(Convert.ToString(item.toTime)))
                        {
                            List<object> objList = new List<object>();
                            objList.Add(item);
                            return objList;
                        }
                    }
                }
                return Json(FormObj);
            }
            catch (Exception ex)
            {

                ModelState.AddModelError(string.Empty, "Please try after sometime");
                return BadRequest(ModelState);
            }

        }
        [DisableCors]
        [HttpGet("GoCameodmstaticremiderupcoming")]
        public object GoCameoDMstaticRemiderUpcoming(string Time)
        {
            try
            {
                string FormStr = "";
                // var _time = String.IsNullOrEmpty(Time) ? null : Time.Substring(0, 5);
                //var timeUtc = DateTime.Parse(Time);
                //TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                //DateTime easternTime = TimeZoneInfo.ConvertTime(timeUtc, easternZone);
                var _time = String.IsNullOrEmpty(Time) ? null : Time.Substring(Time.IndexOf(" ") + 1, 5).Replace(":", ".");
                StreamReader oRead;
                string filepath = System.IO.Path.Combine(_env.ContentRootPath, "wwwroot/assets/js", "staticreminderdm.json");

                if (System.IO.File.Exists(filepath))
                {
                    filepath = System.IO.Path.Combine(_env.ContentRootPath, "wwwroot/assets/js");
                    oRead = System.IO.File.OpenText(Path.Combine(filepath, "staticreminderdm.json"));  //For Local
                    FormStr = oRead.ReadToEnd();
                    oRead.Close();
                }
                dynamic FormObj = JsonConvert.DeserializeObject(FormStr);
                List<object> objList = new List<object>();
                if (!String.IsNullOrEmpty(Time))
                {
                    foreach (var item in FormObj)
                    {
                        if (objList.Count == 2)
                        {
                            return objList;
                        }
                        else if (Convert.ToDouble(Convert.ToString(item.fromTime)) > Convert.ToDouble(_time))
                        {
                            objList.Add(item);
                        }
                    }
                }
                return objList;
            }
            catch (Exception ex)
            {

                ModelState.AddModelError(string.Empty, "Please try after sometime");
                return BadRequest(ModelState);
            }

        }
        [DisableCors]
        [HttpGet("GoCameoddstaticremider")]
        public object GoCameoDDstaticRemider(string Time)
        {
            try
            {
                string FormStr = "";
                // var _time = String.IsNullOrEmpty(Time) ? null : Time.Substring(0, 5);
                //var timeUtc = DateTime.Parse(Time);
                //TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                //DateTime easternTime = TimeZoneInfo.ConvertTime(timeUtc, easternZone);
                var _time = String.IsNullOrEmpty(Time) ? null : Time.Substring(Time.IndexOf(" ") + 1, 5).Replace(":", ".");
                StreamReader oRead;
                string filepath = System.IO.Path.Combine(_env.ContentRootPath, "wwwroot/assets/js", "staticreminderdd.json");

                if (System.IO.File.Exists(filepath))
                {
                    filepath = System.IO.Path.Combine(_env.ContentRootPath, "wwwroot/assets/js");
                    oRead = System.IO.File.OpenText(Path.Combine(filepath, "staticreminderdd.json"));  //For Local
                    FormStr = oRead.ReadToEnd();
                    oRead.Close();
                }
                dynamic FormObj = JsonConvert.DeserializeObject(FormStr);
                if (!String.IsNullOrEmpty(Time))
                {
                    foreach (var item in FormObj)
                    {
                        if (Convert.ToDouble(Convert.ToString(item.fromTime)) <= Convert.ToDouble(_time) && Convert.ToDouble(_time) < Convert.ToDouble(Convert.ToString(item.toTime)))
                        {
                            List<object> objList = new List<object>();
                            objList.Add(item);
                            return objList;
                        }
                    }
                }
                return Json(FormObj);
            }
            catch (Exception ex)
            {

                ModelState.AddModelError(string.Empty, "Please try after sometime");
                return BadRequest(ModelState);
            }

        }
        [DisableCors]
        [HttpGet("GoCameoddstaticremiderupcoming")]
        public object GoCameoDDstaticRemiderUpcoming(string Time)
        {
            try
            {
                string FormStr = "";
                // var _time = String.IsNullOrEmpty(Time) ? null : Time.Substring(0, 5);
                //var timeUtc = DateTime.Parse(Time);
                //TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                //DateTime easternTime = TimeZoneInfo.ConvertTime(timeUtc, easternZone);
                var _time = String.IsNullOrEmpty(Time) ? null : Time.Substring(Time.IndexOf(" ") + 1, 5).Replace(":", ".");
                StreamReader oRead;
                string filepath = System.IO.Path.Combine(_env.ContentRootPath, "wwwroot/assets/js", "staticreminderdd.json");

                if (System.IO.File.Exists(filepath))
                {
                    filepath = System.IO.Path.Combine(_env.ContentRootPath, "wwwroot/assets/js");
                    oRead = System.IO.File.OpenText(Path.Combine(filepath, "staticreminderdd.json"));  //For Local
                    FormStr = oRead.ReadToEnd();
                    oRead.Close();
                }
                dynamic FormObj = JsonConvert.DeserializeObject(FormStr);
                List<object> objList = new List<object>();
                if (!String.IsNullOrEmpty(Time))
                {
                    foreach (var item in FormObj)
                    {
                        if (objList.Count == 2)
                        {
                            return objList;
                        }
                        else if (Convert.ToDouble(Convert.ToString(item.fromTime)) > Convert.ToDouble(_time))
                        {
                            objList.Add(item);
                        }
                    }
                }
                return objList;
            }
            catch (Exception ex)
            {

                ModelState.AddModelError(string.Empty, "Please try after sometime");
                return BadRequest(ModelState);
            }

        }
        [DisableCors]
        [HttpGet("getnumberlist")]
        public object getNumberList(string start, string end)
        {
            try
            {
                dynamic list = JArray.Parse("[]");

                for (var i = Convert.ToInt32(start); i <= Convert.ToInt32(end); i++)
                {
                    dynamic item = JObject.Parse("{}");
                    item.key = i.ToString();
                    item.value = i.ToString();

                    list.Add(item);
                }
                return Json(list);
            }
            catch (Exception ex)
            {

                ModelState.AddModelError(string.Empty, "Please try after sometime");
                return BadRequest(ModelState);
            }

        }
        private static List<string> ParseSearchQuery(string pSearchText)
        {
            var _return = pSearchText;
            try
            {
                Tokenizer _Tokenizer = new Tokenizer();
                List<DslToken> result = _Tokenizer.Tokenize(pSearchText);
                foreach (DslToken _token in result)
                {
                    if (!string.IsNullOrEmpty(_token.Value))
                    {
                        if (_token.Value == "(" || _token.Value == ")")
                        {
                            _return = _return.Replace(_token.Value, ",");
                        }
                        else
                        {
                            _return = _return.Replace(" " + _token.Value + " ", " , ");
                        }
                    }
                }
                _return = _return.Replace("True", "");
                _return = _return.Replace("False", "");
                _return = _return.Replace("true", "");
                _return = _return.Replace("false", "");
                return _return.Split(",").Where(x => x.Trim() != "").ToList();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}