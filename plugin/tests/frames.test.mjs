import test from 'node:test';import assert from 'node:assert/strict';import {Frames} from '../dist/frames.js';
test('split pipe messages remain buffered until newline',()=>{const f=new Frames();assert.deepEqual(f.push('{"version":'),[]);assert.deepEqual(f.push('1}\n'),[{version:1}]);});
test('oversized frames are rejected even without a newline',()=>{assert.throws(()=>new Frames().push('x'.repeat(65537)),/message_too_large/);});
test('malformed pipe JSON is rejected instead of treated as fresh data',()=>{assert.throws(()=>new Frames().push('not json\n'),/invalid_json/);});
