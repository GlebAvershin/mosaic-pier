import fs from 'node:fs';
import assert from 'node:assert/strict';
const { instance }=await WebAssembly.instantiate(fs.readFileSync('web/pier.wasm'));
const r=instance.exports;
for(let i=0;i<r.mp_count();i++){
  r.mp_start(i);
  while(r.mp_status()===0){
    const p=r.mp_hint();
    assert.ok(r.mp_swap(Math.floor(p/36),p%36));
    while(r.mp_status()===1)r.mp_step();
  }
  assert.equal(r.mp_status(),2);
  console.log(i,r.mp_moves(),r.mp_score());
}
