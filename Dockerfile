FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine
COPY . /src
RUN cd /src/BililiveRecorder.Cli && dotnet build -o /output -c Release

FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine
RUN apk add --no-cache tzdata
ENV TZ=Asia/Shanghai
COPY --from=0 /output /app
VOLUME [ "/rec" ]
WORKDIR /app
ENTRYPOINT [ "/app/BililiveRecorder.Cli" ]
CMD [ "run", "/rec" ]
