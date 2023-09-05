using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public class Program
{
    public static bool Log;
    public static void Main()
    {
        using (var ctx = new ExampleContext())
        {
            ctx.Database.Migrate();
        }
        SetupData();

        Log = true;
        Console.WriteLine("\n\n First Time: \n\n");
        WeirdBehaviour();
        Console.WriteLine("\n\n Second Time: \n\n");
        WeirdBehaviour();
        Log = false;

        Cleanup();
    }
    private static void WeirdBehaviour()
    {
        using var ctx = new ExampleContext();
        var artists = ctx.Artists.First();
        var songs = ctx.Songs.ToList();

        artists.Songs.Clear();

        foreach (var song in songs)
            artists.Songs.Add(song);

        ctx.SaveChanges();
    }
    private static void Cleanup()
    {
        using var ctx = new ExampleContext();
        ctx.Songs.ExecuteDelete();
        ctx.Artists.ExecuteDelete();
    }
    private static void SetupData()
    {
        using var ctx = new ExampleContext();
        var artist = new Artist();
        ctx.Artists.Add(artist);
        ctx.SaveChanges();

        for (int i = 0; i < 3; i++)
            artist.Songs.Add(new Song());

        ctx.SaveChanges();
    }
}

public class Artist : INotifyPropertyChanged, INotifyPropertyChanging
{
    private long _id;

    public long Id
    {
        get => _id;
        set
        {
            if (_id != value)
            {
                this.OnPropertyChanging();
                this._id = value;
                this.OnPropertyChanged();
            }
        }
    }

    private ICollection<Song> _songs;

    public ICollection<Song> Songs
    {
        get => lazyLoader.Load(this, ref _songs);
    }

    protected readonly Action<object, string> lazyLoader;

    public Artist()
    {

    }

    public Artist(Action<object, string> lazyLoader)
    {
        this.lazyLoader = lazyLoader;
    }

    public event PropertyChangingEventHandler PropertyChanging;
    public event PropertyChangedEventHandler PropertyChanged;

    public virtual void OnPropertyChanging([CallerMemberName] string propertyName = null)
    {
        if (PropertyChanging != null)
        {
            PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
        }
    }
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        if (PropertyChanged != null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}

public class Song : INotifyPropertyChanged, INotifyPropertyChanging
{
    private long _id;

    public long Id
    {
        get => _id;
        set
        {
            if (_id != value)
            {
                this.OnPropertyChanging();
                this._id = value;
                this.OnPropertyChanged();
            }
        }
    }

    private ICollection<Artist> _artists;

    public ICollection<Artist> Artists
    {
        get => lazyLoader.Load(this, ref _artists);
    }

    protected readonly Action<object, string> lazyLoader;

    public Song()
    {
        
    }

    public Song(Action<object, string> lazyLoader)
    {
        this.lazyLoader = lazyLoader;
    }

    public event PropertyChangingEventHandler PropertyChanging;
    public event PropertyChangedEventHandler PropertyChanged;

    public virtual void OnPropertyChanging([CallerMemberName] string propertyName = null)
    {
        if (PropertyChanging != null)
        {
            PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
        }
    }
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        if (PropertyChanged != null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

public class ArtistSong : INotifyPropertyChanged, INotifyPropertyChanging
{
    private long _songId;

    public long SongId
    {
        get => _songId;
        set
        {
            if (_songId != value)
            {
                this.OnPropertyChanging();
                this._songId = value;
                this.OnPropertyChanged();
            }
        }
    }
    private long _artistId;

    public long ArtistId
    {
        get => _artistId;
        set
        {
            if (_artistId != value)
            {
                this.OnPropertyChanging();
                this._artistId = value;
                this.OnPropertyChanged();
            }
        }
    }
    
    private Artist _artist;

    public Artist Artist
    {
        get => _artist;
        set
        {
            if (_artist != value)
            {
                this.OnPropertyChanging();
                this._artist = value;
                this.OnPropertyChanged();
            }
        }
    }
    private Song _song;

    public Song Song
    {
        get => _song;
        set
        {
            if (_song != value)
            {
                this.OnPropertyChanging();
                this._song = value;
                this.OnPropertyChanged();
            }
        }
    }

    public event PropertyChangingEventHandler PropertyChanging;
    public event PropertyChangedEventHandler PropertyChanged;

    public virtual void OnPropertyChanging([CallerMemberName] string propertyName = null)
    {
        if (PropertyChanging != null)
        {
            PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
        }
    }
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        if (PropertyChanged != null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

public class ExampleContext : DbContext
{
    public DbSet<Artist> Artists { get; set; }
    public DbSet<Song> Songs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("Server=.;Database=WeirdBehaviour;User Id=dev;Password=admin;TrustServerCertificate=True;Trusted_Connection=True");
        optionsBuilder.EnableDetailedErrors();
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.EnableThreadSafetyChecks();
        optionsBuilder.LogTo(x =>
        {
            if (!Program.Log)
                return;

            //var check = x.ToLower();
            //if (check.Contains("insert") || check.Contains("update") || check.Contains("delete"))
            if (x.Contains("Executing DbCommand"))
            Console.WriteLine(x);
        });
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasChangeTrackingStrategy(ChangeTrackingStrategy.ChangingAndChangedNotifications);

        modelBuilder.Entity<Artist>()
            .HasMany(x => x.Songs)
            .WithMany(x => x.Artists)
            .UsingEntity<ArtistSong>(
                r => r.HasOne(x => x.Song).WithMany().HasForeignKey(x => x.SongId).OnDelete(DeleteBehavior.Cascade),
                l => l.HasOne(x => x.Artist).WithMany().HasForeignKey(x => x.ArtistId).OnDelete(DeleteBehavior.Cascade),
                t => t.ToTable("ArtistSong")
            );
    }
}


public static class PocoLoadingExtensions
{
    public static ICollection<TRelated> Load<TRelated>(
        this Action<object, string> loader,
        object entity,
        ref ICollection<TRelated> navigationField,
        [CallerMemberName] string navigationName = null)
        where TRelated : class
    {
        loader?.Invoke(entity, navigationName);
        navigationField ??= new ObservableCollection<TRelated>();

        return navigationField;
    }
}
