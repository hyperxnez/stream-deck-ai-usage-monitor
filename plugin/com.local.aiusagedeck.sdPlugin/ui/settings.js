let ws,context,actionId,current={};const fields=['provider','window','group'];
function send(event,payload){if(ws?.readyState===1)ws.send(JSON.stringify({event,context,action:actionId,payload}));}
function query(){send('sendToPlugin',{request:'status',provider:current.provider||document.getElementById('provider').value});}
function apply(){for(const field of fields)document.getElementById(field).value=current[field]||'';}
window.connectElgatoStreamDeckSocket=function(port,uuid,registerEvent,info,actionInfo){
 context=uuid;const action=JSON.parse(actionInfo);actionId=action.action;current=action.payload.settings||{};
 ws=new WebSocket('ws://127.0.0.1:'+port);ws.onopen=()=>{ws.send(JSON.stringify({event:registerEvent,uuid}));apply();query();};
 ws.onmessage=e=>{const message=JSON.parse(e.data);if(message.event==='didReceiveSettings'){current=message.payload.settings||{};apply();query();}
 if(message.event==='sendToPropertyInspector'){const group=document.getElementById('group');group.replaceChildren();const def=new Option('Nhóm mặc định','');group.add(def);for(const item of message.payload.groups||[])group.add(new Option(item.name,item.id));group.value=current.group||'';
 const checked=message.payload.observedAt?new Date(message.payload.observedAt).toLocaleString('vi-VN'):'Chưa có dữ liệu';document.getElementById('status').textContent=(message.payload.state||'offline')+' · '+checked+(message.payload.errorCode?' · '+message.payload.errorCode:'');}};
};
for(const field of fields)document.getElementById(field).addEventListener('change',()=>{if(field==='provider')document.getElementById('group').value='';current={provider:document.getElementById('provider').value,window:document.getElementById('window').value,group:document.getElementById('group').value};send('setSettings',current);query();});
document.getElementById('check').addEventListener('click',query);
