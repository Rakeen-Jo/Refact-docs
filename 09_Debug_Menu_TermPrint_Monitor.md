# 09. Debug Menu / Term Print / Monitor

## 목적
- 문제를 "느낌"이 아니라 카운터/지표로 확인
- 포트별/경로별 이상 징후를 빠르게 분리

## 구성 요약
- `term_print.c`: UART/TCP/CAN 출력 라우팅
- `V5B2_main.c`: monitor 주기 출력, mode bit 기반 표시
- monitor mode 예시: `CycleRT`, UART/Sensor UART 상태 등

## 핵심 필드 해석
- `WRtx/MRtx/MRdone`: 모터 write/read 송수신 진행
- `SRtx/SRdone`: 센서 read 요청 대비 완료
- `Stmo/cut/dead`: timeout/중단/deadlock 개입
- `UErr/UORE`: UART 에러 복구 지표
- `Sid/Slen/Scrc`: 마지막 센서 프레임 품질 단서

## 운영 룰 (권장)
1. 절대값보다 interval delta(증가량) 중심 분석
2. `SRtx-SRdone` 갭이 지속 확대되면 즉시 포트 분리 점검
3. `UErr`와 `dead` 동시 증가 시 라인/타이밍 문제 우선
4. 로그 과다 출력은 실시간성에 영향을 주므로 모드 최소화

## 바로 붙여넣기용 점검 템플릿
- 관찰 시간: 
- 모드/interval: 
- 이상 포트: 
- `SRtx/SRdone/Stmo/cut/dead/UErr` 증가량:
- 직전 변경 커밋:
- 결론/다음 액션:
