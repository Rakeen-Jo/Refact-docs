# 06. Sensor Timing Stabilization

## 목표
- sensor read 성공률 상승
- timeout(`Stmo`) / cut / dead 최소화
- 포트별 편차(U1/U3/U4/U6) 관리

## 최근 관측 요약
- U3/U4/U6: `SRtx≈SRdone`, `Stmo/cut/dead` 거의 0으로 수렴하는 구간 확인
- U1: 간헐적으로 `Stmo/cut/dead/UErr` 증가가 집중

## 카운터 의미 (중요)
- `Stmo`: sensor timeout 누적
- `cut`: 센서 라운드 중단(abort/late-cut 포함)
- `dead`: RX deadlock watchdog 개입 횟수
- `UErr`: UART error recover 횟수
- `Sid/Slen`: 마지막 유효 sensor ID/길이

## ESP-IDF 센서 FW 대응 내역
- turnaround delay
- stale TX_DONE clear
- explicit reply delay
- late-cut (`REPLY_LATE_US`)
- TX_DONE lost/stuck guard

## 해석 규칙
1. 절대값이 아니라 interval delta(증가량)로 본다
2. `UErr`와 `dead` 동반 상승 시 recover 타이밍/라인 품질 이슈 우선
3. `Sid`는 마지막 성공 ID이므로 포트별로 달라도 정상

## 포트별 기록 템플릿
| Port | SRtx | SRdone | Stmo | cut | dead | UErr | Sid | 비고 |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| U1 |  |  |  |  |  |  |  |  |
| U3 |  |  |  |  |  |  |  |  |
| U4 |  |  |  |  |  |  |  |  |
| U6 |  |  |  |  |  |  |  |  |
