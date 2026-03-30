# V5B2 IAP Updater (Console, .NET)

자동 시퀀스:
1. COM 오픈
2. SPACE 전송 (boot countdown break)
3. `Input Password` 대기
4. `wonik1234` 전송
5. `Main Menu` 대기
6. `1` 전송 (Download)
7. YMODEM 전송
8. `Programming Completed Successfully` 대기

## Build
```bash
dotnet build
```

## Run
```bash
dotnet run -- COM5 path/to/fw.bin 921600
```

- 기본 baud는 921600 (인자 생략 시)
- 전송 실패 시 자동 재시도(최대 3회)


## 참고
- 대상 부트로더는 `F429_IAP` 코드 기준
- 메뉴 문자열이 바뀌면 `Program.cs`의 `WaitContains()` 토큰 수정 필요
- 필요 시 비밀번호를 코드 상수에서 변경
