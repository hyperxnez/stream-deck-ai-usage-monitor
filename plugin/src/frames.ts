export class Frames {
 private buffer='';
 push(chunk:string):unknown[]{
  this.buffer+=chunk;const frames:unknown[]=[];
  let end:number;
  while((end=this.buffer.indexOf('\n'))>=0){
   if(end>65536)throw new Error('message_too_large');
   const line=this.buffer.slice(0,end);this.buffer=this.buffer.slice(end+1);
   try{frames.push(JSON.parse(line));}catch{throw new Error('invalid_json');}
  }
  if(this.buffer.length>65536)throw new Error('message_too_large');return frames;
 }
}
