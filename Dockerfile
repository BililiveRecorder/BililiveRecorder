FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine
COPY . /src
RUN cd /src/BililiveRecorder.Cli && dotnet build -o /output -c Release

FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine
RUN apk add --no-cache tzdata
ENV TZ=Asia/Shanghai
WORKDIR /app
VOLUME [ "/rec" ]
COPY --from=0 /output /app
ENTRYPOINT [ "/app/BililiveRecorder.Cli" ]
EXPOSE 2356/tcp
CMD [ "run", "--bind", "http://*:2356", "/rec" ]
