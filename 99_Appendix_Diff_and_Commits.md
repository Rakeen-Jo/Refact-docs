# 99. Appendix - Diff & Commits

## 비교 대상
- Before: `work/02_V5B2_SENSE`
- After: `work/02_V5B2_SENSE_Refact_2ea0133`

## 빠른 비교 커맨드
```bash
# 1) 디렉터리 비교( .git 제외 )
diff -ruN --exclude=.git work/02_V5B2_SENSE work/02_V5B2_SENSE_Refact_2ea0133 > /tmp/v5b2_full.diff

# 2) 변경 파일 목록 요약
diff -rq --exclude=.git work/02_V5B2_SENSE work/02_V5B2_SENSE_Refact_2ea0133

# 3) 핵심 파일 비교
diff -u work/02_V5B2_SENSE/Core/Src/main.c work/02_V5B2_SENSE_Refact_2ea0133/Core/Src/main.c

diff -u work/02_V5B2_SENSE/Core/Src/xc330-t181-dma.c work/02_V5B2_SENSE_Refact_2ea0133/V5B2/Src/motor_xc330_dma.c

diff -u work/02_V5B2_SENSE/Core/Src/xc330-t181.c work/02_V5B2_SENSE_Refact_2ea0133/V5B2/Src/motor_xc330.c

diff -u work/02_V5B2_SENSE/Core/Src/Interface.c work/02_V5B2_SENSE_Refact_2ea0133/V5B2/Src/can_comm.c
```

## 함수 목록 자동 추출(빠른 맵핑용)
```bash
python3 - << 'PY'
import re, pathlib
p='work/02_V5B2_SENSE_Refact_2ea0133/V5B2/Src/motor_xc330_dma.c'
pat=re.compile(r'^\s*(?:static\s+)?(?:inline\s+)?(?:void|bool|int|uint\w+_t|BaseType_t|HAL_StatusTypeDef)\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(')
for line in pathlib.Path(p).read_text(errors='ignore').splitlines():
    m=pat.match(line)
    if m: print(m.group(1))
PY
```

## 우선 점검 파일(Refact)
- `Core/Src/main.c`
- `Core/Src/stm32f4xx_it.c`
- `Core/Src/stm32f4xx_hal_msp.c`
- `V5B2/Src/motor_ctrl.c`
- `V5B2/Src/motor_xc330_dma.c`
- `V5B2/Src/can_comm.c`
- `V5B2/Src/ethernet.c`
- `V5B2/Src/term_print.c`
- `V5B2/Src/V5B2_main.c`

## 병렬 비교 대상
- PC SW: `work/V5B2_Software`
- ESP-IDF sensor FW: `work/Finger_ESP32`
- Arduino legacy sensor FW:
  - `work/02_ESP32C3_DEV_madi_20251113`
  - `work/02_ESP32C3_DEV_Robotfinger_B2_20260123`

## Commit 맵 템플릿
| Commit | Repo | Area | What changed | Why | Verification |
|---|---|---|---|---|---|
| e8b4a28 | 02_V5B2_SENSE_Refact | Motor/DMA | 계측+가드 | freeze 원인 분리 | crash tag/log |
| d47549f | 02_V5B2_SENSE_Refact | Init | HAL_Delay->osDelay | busy-wait 제거 | init 출력/응답 |
| 6bd5aa8 | 02_V5B2_SENSE_Refact | CAN IRQ | priority 수정 | FromISR 규칙 충족 | CAN 연결 안정성 |
| 8f43ee0 | 02_V5B2_SENSE_Refact | CAN TX | busy-wait 제거 | starvation 방지 | RED LED/task liveness |
| c5379b7 | Finger_ESP32 | UART ISR | stale TX_DONE clear | half-duplex race 완화 | SRdone 증가 |
| 69f9b6a | Finger_ESP32 | Sensor timing | late-cut 추가 | cycle 침범 방지 | Stmo/cut 변화 |
